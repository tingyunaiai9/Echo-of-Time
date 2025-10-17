/* Editor/NetworkManagerSetup.cs
 * NetworkManager 编辑器工具
 * 帮助快速设置 NetworkManager 组件
 */

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Mirror;

[CustomEditor(typeof(EchoNetworkManager))]
public class NetworkManagerSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EchoNetworkManager networkManager = (EchoNetworkManager)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("快速设置", EditorStyles.boldLabel);
        
        if (GUILayout.Button("验证组件配置"))
        {
            ValidateSetup(networkManager);
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "确保已添加以下组件:\n" +
            "1. RelayTransportMirror\n" +
            "2. QuickNetworkTest (可选，用于快速测试)\n\n" +
            "测试按键:\n" +
            "H - 创建房间\n" +
            "C - 加入房间\n" +
            "L - 离开房间\n" +
            "R - 显示房间信息",
            MessageType.Info
        );
    }
    
    private void ValidateSetup(EchoNetworkManager networkManager)
    {
        bool hasErrors = false;
        
        // 检查 RelayTransportMirror
        var relayTransport = networkManager.GetComponent<Unity.Sync.Relay.Transport.Mirror.RelayTransportMirror>();
        if (relayTransport == null)
        {
            Debug.LogError("[Setup] 缺少 RelayTransportMirror 组件!");
            hasErrors = true;
        }
        else
        {
            Debug.Log("[Setup] ✓ RelayTransportMirror 已配置");
        }
        
        // 检查 Transport 设置
        if (networkManager.transport == null)
        {
            Debug.LogError("[Setup] Transport 未设置!");
            hasErrors = true;
        }
        else if (relayTransport != null && networkManager.transport != relayTransport as Mirror.Transport)
        {
            Debug.LogWarning("[Setup] Transport 应该设置为 RelayTransportMirror");
            hasErrors = true;
        }
        else
        {
            Debug.Log("[Setup] ✓ Transport 已正确设置");
        }
        
        // 检查玩家预制体
        if (networkManager.playerPrefab == null)
        {
            Debug.LogWarning("[Setup] 未设置 Player Prefab (可选)");
        }
        else
        {
            // 检查玩家预制体是否有 NetworkIdentity
            if (networkManager.playerPrefab.GetComponent<NetworkIdentity>() == null)
            {
                Debug.LogError("[Setup] Player Prefab 缺少 NetworkIdentity 组件!");
                hasErrors = true;
            }
            else
            {
                Debug.Log("[Setup] ✓ Player Prefab 已配置");
            }
        }
        
        // 检查测试脚本
        var quickTest = networkManager.GetComponent<QuickNetworkTest>();
        if (quickTest == null)
        {
            Debug.LogWarning("[Setup] 建议添加 QuickNetworkTest 组件用于快速测试");
        }
        else
        {
            Debug.Log("[Setup] ✓ QuickNetworkTest 已添加");
        }
        
        if (!hasErrors)
        {
            Debug.Log("[Setup] ========== 配置验证完成 ==========");
            EditorUtility.DisplayDialog(
                "验证完成",
                "NetworkManager 配置正确!\n\n可以开始测试网络功能。",
                "确定"
            );
        }
        else
        {
            EditorUtility.DisplayDialog(
                "验证失败",
                "发现配置问题，请查看 Console 了解详情。",
                "确定"
            );
        }
    }
}
#endif
