using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using Events;

public class ImageNetworkSender : NetworkBehaviour
{
    public static ImageNetworkSender LocalInstance;

    // 缓存正在接收的碎片： imageId -> (index -> data)
    private Dictionary<int, Dictionary<int, byte[]>> receiveBuffer = new Dictionary<int, Dictionary<int, byte[]>>();

    // 防止自己收到自己发的图片导致重复显示
    private HashSet<int> sentImageIds = new HashSet<int>();

    // 分块大小配置
    private const int CHUNK_SIZE = 1024 * 16;

    public override void OnStartLocalPlayer()
    {
        LocalInstance = this;
    }

    public void SendImage(byte[] imageData, int timeline, int level, string imageType = "Chat")
    {
        if (imageData == null || imageData.Length == 0) return;

        // 生成唯一ID
        int imageId = UnityEngine.Random.Range(0, int.MaxValue);
        
        // 记录自己发送的ID
        sentImageIds.Add(imageId);

        // 计算分块
        int totalChunks = Mathf.CeilToInt(imageData.Length / (float)CHUNK_SIZE);

        StartCoroutine(SendChunksProcess(imageId, totalChunks, timeline, level, imageData, imageType));
    }

    private IEnumerator SendChunksProcess(int imageId, int totalChunks, int timeline, int level, byte[] fullData, string imageType)
    {
        for (int i = 0; i < totalChunks; i++)
        {
            int offset = i * CHUNK_SIZE;
            int length = Mathf.Min(CHUNK_SIZE, fullData.Length - offset);

            byte[] chunk = new byte[length];
            Array.Copy(fullData, offset, chunk, 0, length);

            // 发送给服务器
            CmdSendChunk(new NetworkMessageTypes.ImageChunkMessage
            {
                imageId = imageId,
                chunkIndex = i,
                totalChunks = totalChunks,
                timeline = timeline,
                level = level,
                chunkData = chunk,
                imageType = imageType
            });
            if (i % 5 == 0) yield return null;
        }
        Debug.Log($"[Network] 图片发送完毕 ID: {imageId}, 总块数: {totalChunks}, 类型: {imageType}");
    }

    [Command]
    private void CmdSendChunk(NetworkMessageTypes.ImageChunkMessage msg)
    {
        // 服务器收到后，直接转发给所有客户端
        RpcReceiveChunk(msg);
    }

    [ClientRpc]
    private void RpcReceiveChunk(NetworkMessageTypes.ImageChunkMessage msg)
    {
        // 如果是自己发送的，直接忽略
        if (sentImageIds.Contains(msg.imageId)) return;

        if (!receiveBuffer.ContainsKey(msg.imageId))
        {
            receiveBuffer[msg.imageId] = new Dictionary<int, byte[]>();
        }

        receiveBuffer[msg.imageId][msg.chunkIndex] = msg.chunkData;

        if (receiveBuffer[msg.imageId].Count == msg.totalChunks)
        {
            Debug.Log($"[Network] 图片接收完整 ID: {msg.imageId}");
            ReassembleAndShow(msg.imageId, msg.totalChunks, msg.timeline, msg.level, msg.imageType);
        }
    }

    private void ReassembleAndShow(int imageId, int totalChunks, int timeline, int level, string imageType)
    {
        if (!receiveBuffer.ContainsKey(imageId)) return;

        var chunks = receiveBuffer[imageId];

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

        if (imageType == "Clue")
        {
            ClueBoard.AddClueEntry(timeline, level, fullImage, publish: false);
        }
        else
        {
            DialogPanel.AddChatImage(fullImage, timeline, publish: false);
        }
    }
}