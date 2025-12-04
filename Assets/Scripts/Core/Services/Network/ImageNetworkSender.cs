using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public struct ImageChunkMessage : NetworkMessage
{
    public int imageId;       // 图片的唯一ID，防止多张图混淆
    public int chunkIndex;    // 当前是第几块
    public int totalChunks;   // 总共有几块
    public int timeline;      // 你的业务逻辑需要的 timeline 参数
    public byte[] chunkData;  // 实际的切片数据
}
public class ImageNetworkSender : NetworkBehaviour
{
    // 单例方便调用（如果是本地玩家）
    public static ImageNetworkSender LocalInstance;

    // 缓存正在接收的碎片： imageId -> (index -> data)
    private Dictionary<int, Dictionary<int, byte[]>> receiveBuffer = new Dictionary<int, Dictionary<int, byte[]>>();

    // 配置分块大小：Mirror 默认 MTU 约 1200字节，安全起见每块建议 < 100KB (如果用Reliable)
    // 建议设置为 16KB - 32KB，既不会太大导致阻塞，也不会太小导致头部开销过大
    private const int CHUNK_SIZE = 1024 * 16;

    public override void OnStartLocalPlayer()
    {
        LocalInstance = this;
    }

    /// <summary>
    /// 对外接口：发送图片
    /// </summary>
    public void SendImage(byte[] imageData, int timeline)
    {
        if (imageData == null || imageData.Length == 0) return;

        // 生成唯一ID
        int imageId = UnityEngine.Random.Range(0, int.MaxValue);

        // 计算分块
        int totalChunks = Mathf.CeilToInt(imageData.Length / (float)CHUNK_SIZE);

        StartCoroutine(SendChunksProcess(imageId, totalChunks, timeline, imageData));
    }

    private IEnumerator SendChunksProcess(int imageId, int totalChunks, int timeline, byte[] fullData)
    {
        for (int i = 0; i < totalChunks; i++)
        {
            int offset = i * CHUNK_SIZE;
            int length = Mathf.Min(CHUNK_SIZE, fullData.Length - offset);

            byte[] chunk = new byte[length];
            Array.Copy(fullData, offset, chunk, 0, length);

            // 发送给服务器
            CmdSendChunk(new ImageChunkMessage
            {
                imageId = imageId,
                chunkIndex = i,
                totalChunks = totalChunks,
                timeline = timeline,
                chunkData = chunk
            });

            // 关键：每发送几块暂停一下，防止瞬间塞满带宽导致卡顿
            // 如果图片很大，可以每 5-10 块 yield return null
            if (i % 5 == 0) yield return null;
        }
        Debug.Log($"[Network] 图片发送完毕 ID: {imageId}, 总块数: {totalChunks}");
    }

    [Command]
    private void CmdSendChunk(ImageChunkMessage msg)
    {
        // 服务器收到后，直接转发给所有客户端
        RpcReceiveChunk(msg);
    }

    [ClientRpc]
    private void RpcReceiveChunk(ImageChunkMessage msg)
    {
        // 1. 初始化缓存容器
        if (!receiveBuffer.ContainsKey(msg.imageId))
        {
            receiveBuffer[msg.imageId] = new Dictionary<int, byte[]>();
        }

        // 2. 存入当前分块
        receiveBuffer[msg.imageId][msg.chunkIndex] = msg.chunkData;

        // 3. 检查是否接收完整
        if (receiveBuffer[msg.imageId].Count == msg.totalChunks)
        {
            Debug.Log($"[Network] 图片接收完整 ID: {msg.imageId}");
            ReassembleAndShow(msg.imageId, msg.totalChunks, msg.timeline);
        }
    }

    private void ReassembleAndShow(int imageId, int totalChunks, int timeline)
    {
        if (!receiveBuffer.ContainsKey(imageId)) return;

        var chunks = receiveBuffer[imageId];

        // 计算总大小
        int totalSize = 0;
        for (int i = 0; i < totalChunks; i++)
        {
            if (chunks.ContainsKey(i))
                totalSize += chunks[i].Length;
            else
            {
                Debug.LogError("缺页，无法重组！");
                return;
            }
        }

        // 合并字节数组
        byte[] fullImage = new byte[totalSize];
        int offset = 0;
        for (int i = 0; i < totalChunks; i++)
        {
            byte[] piece = chunks[i];
            Array.Copy(piece, 0, fullImage, offset, piece.Length);
            offset += piece.Length;
        }

        // 清理缓存
        receiveBuffer.Remove(imageId);

        // *** 调用你的 UI 面板显示图片 ***
        // 注意：这里需要切回主线程逻辑（Rpc 默认在主线程，所以是安全的）
        DialogPanel.AddChatImage(fullImage, timeline, publish: false);
        // publish=false 很重要，因为这已经是广播收到的结果了，不需要再发布事件去触发网络发送
    }
}