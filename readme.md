# Echo of Time

清华大学软件学院2025秋《软件工程》课程

三四五组大作业：Echo of Time

**一款结合 AIGC 的三人联机跨时空合作解谜游戏**

> 若生命注定短暂，你会沉湎，会直面，还是会淡漠？又或者，你会在真切地体会过一切遗憾和悲喜，甚至体验过永不落下的烈日之后明白，生命的意义在于每一个热烈的当下。

## 游戏简介

**Echo of Time** 是一款主打“跨越时空”与“非对称解谜”的三人合作游戏。三名玩家将分别身处**古代、民国和现代**三条平行时间线，在同一个物理空间内进行探索。通过巧妙的因果律交互与 AIGC 赋能的交流机制，玩家需要通力合作，共享线索，解开跨越千年的谜题。

目前游戏已发布稳定版本，包含 **3 大关卡层、9 个独立场景、10+ 趣味小游戏**以及 **18 段完整剧情演绎**。

## 技术架构



| **模块**      | **技术栈 / 工具**      | **描述**                                                     |
| ------------- | ---------------------- | ------------------------------------------------------------ |
| **客户端层**  | Unity, LeanTween       | 负责核心逻辑驱动与丝滑的动画演出                             |
| **网络层**    | Mirror, UOS Sync Relay | 基于 Host-Client 架构，保障三人实时稳定联机                  |
| **AI 服务层** | DeepSeek API, 即梦 API | 实时处理跨时空日记交流，实现文本到诗句、代码、图像的跨模态转换 |
| **项目管理**  | GitHub, 飞书, Overleaf | 规范化的代码托管与文档协同                                   |

## 快速开始

- 稳定版下载（Release）：https://github.com/tingyunaiai9/Echo-of-Time/releases/latest
- 历史版本与更新日志：https://github.com/tingyunaiai9/Echo-of-Time/releases

## 演示视频（YouTube）

[![Echo of Time Demo](https://img.youtube.com/vi/IQ97gqi-Rz0/maxresdefault.jpg)](https://youtu.be/IQ97gqi-Rz0)

