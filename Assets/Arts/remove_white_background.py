import os
import sys

# 尝试导入 Pillow 库
try:
    from PIL import Image
except ImportError:
    print("错误: 未安装 Pillow 库。")
    print("请在终端运行以下命令安装: pip install Pillow")
    input("按回车键退出...")
    sys.exit(1)

def remove_white_bg(image_path, threshold=240):
    """
    去除图片的白色背景
    :param image_path: 图片路径
    :param threshold: 白色阈值 (0-255)，高于此值的像素将被视为白色并变为透明
    """
    try:
        img = Image.open(image_path)
        img = img.convert("RGBA")
        datas = img.getdata()

        newData = []
        for item in datas:
            # item 是一个元组 (R, G, B, A)
            # 检查 RGB 值是否都大于阈值
            if item[0] > threshold and item[1] > threshold and item[2] > threshold:
                # 变为全透明
                newData.append((255, 255, 255, 0))
            else:
                newData.append(item)

        img.putdata(newData)
        img.save(image_path, "PNG")
        print(f"[成功] 已处理: {image_path}")
    except Exception as e:
        print(f"[失败] 无法处理 {image_path}: {e}")

def main():
    # 获取脚本所在目录
    default_dir = os.path.dirname(os.path.abspath(__file__))
    
    print("此脚本将把所有 PNG 图片的白色背景转换为透明。")
    print("注意: 原文件将被覆盖。")

    # 允许用户输入路径
    user_input = input(f"请输入要扫描的文件夹路径 (留空默认使用脚本所在目录: {default_dir}): ").strip()
    
    target_dir = user_input if user_input else default_dir

    if not os.path.exists(target_dir):
        print(f"错误: 路径不存在: {target_dir}")
        return

    print(f"即将扫描目录: {target_dir}")
    
    confirm = input("是否继续? (y/n): ")
    if confirm.lower() != 'y':
        print("操作已取消。")
        return

    count = 0
    for root, dirs, files in os.walk(target_dir):
        for file in files:
            if file.lower().endswith(".png"):
                file_path = os.path.join(root, file)
                remove_white_bg(file_path)
                count += 1
    
    print(f"完成。共处理了 {count} 张图片。")
    input("按回车键退出...")

if __name__ == "__main__":
    main()
