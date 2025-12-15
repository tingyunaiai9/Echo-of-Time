import os
import sys

# 尝试导入 Pillow 库
try:
    from PIL import Image, ImageChops
except ImportError:
    print("错误: 未安装 Pillow 库。")
    print("请在终端运行以下命令安装: pip install Pillow")
    input("按回车键退出...")
    sys.exit(1)

def auto_crop(image_path):
    """
    自动裁剪图片，去除周围的透明或白色空白区域
    """
    try:
        img = Image.open(image_path)
        img = img.convert("RGBA")
        
        # 1. 尝试基于 Alpha 通道获取边界框 (针对透明背景)
        # split() 返回 (R, G, B, A)
        alpha = img.split()[-1]
        bbox = alpha.getbbox()
        
        # 2. 如果 Alpha 通道全不透明 (bbox 等于原图大小 或 None)，尝试检测白色背景
        # 注意：getbbox() 返回 (left, upper, right, lower)
        is_full_opaque = bbox == (0, 0, img.width, img.height)
        
        if bbox is None:
            # 全透明图片
            print(f"[跳过] 全透明图片: {image_path}")
            return

        if is_full_opaque:
            # 可能是白底图片，尝试基于颜色差异裁剪
            # 创建一个纯白底图
            bg = Image.new("RGBA", img.size, (255, 255, 255, 255))
            # 计算差异 (非白区域会有差异值)
            diff = ImageChops.difference(img, bg)
            # 获取差异区域的 bbox
            # diff 需要转为 RGB 或 L 才能准确 getbbox (因为 Alpha 差异可能为0)
            diff = ImageChops.add(diff, diff, 2.0, -100) # 增强对比度，可选
            bbox = diff.getbbox()
        
        if bbox:
            # 检查裁剪后的尺寸是否变化
            if bbox != (0, 0, img.width, img.height):
                cropped_img = img.crop(bbox)
                cropped_img.save(image_path, "PNG")
                print(f"[裁剪成功] {image_path} | 原尺寸: {img.size} -> 新尺寸: {cropped_img.size}")
            else:
                print(f"[无需裁剪] {image_path}")
        else:
            print(f"[跳过] 无法检测到有效内容 (可能是全白或全透): {image_path}")

    except Exception as e:
        print(f"[失败] 处理出错 {image_path}: {e}")

def main():
    # 获取脚本所在目录
    default_dir = os.path.dirname(os.path.abspath(__file__))
    
    print("此脚本将自动裁剪 PNG 图片，去除周围的透明或白色空白区域。")
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
                auto_crop(file_path)
                count += 1
    
    print(f"完成。共扫描了 {count} 张图片。")
    input("按回车键退出...")

if __name__ == "__main__":
    main()
