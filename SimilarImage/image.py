import os
from pathlib import Path
from collections import defaultdict
from PIL import Image
import imagehash
from concurrent.futures import ThreadPoolExecutor, as_completed
import threading

# ==================== 配置区域 ====================
FOLDER_PATH = r"C:\Users\Admin\Desktop\work\images"  # 请修改为您的图片文件夹路径
HASH_DISTANCE_THRESHOLD = 10  # 汉明距离阈值（≤10视为重复）
SUPPORTED_FORMATS = {".jpg", ".jpeg", ".png", ".bmp", ".gif"}

# phash和dhash的权重分配（两种算法都检测，提高准确性）
PHASH_WEIGHT = 0.5
DHASH_WEIGHT = 0.5

# 多线程配置
MAX_WORKERS = 8  # 根据CPU核心数调整，建议设置为 CPU核心数 或 略高
# ================================================


def get_image_hashes(image_path):
    """计算图片的pHash和dHash值"""
    try:
        img = Image.open(image_path)
        # 统一转换为RGB模式（处理PNG透明通道、GIF等）
        if img.mode != "RGB":
            img = img.convert("RGB")

        phash = imagehash.phash(img)
        dhash = imagehash.dhash(img)
        return image_path, (phash, dhash)
    except Exception as e:
        print(f"警告：无法处理图片 {image_path}: {e}")
        return image_path, None


def find_duplicates(folder_path):
    """查找重复图片（多线程优化版）"""
    folder = Path(folder_path)
    if not folder.exists():
        print(f"错误：文件夹 {folder_path} 不存在！")
        return []

    # 收集所有图片文件
    image_files = []
    for ext in SUPPORTED_FORMATS:
        image_files.extend(folder.rglob(f"*{ext}"))
        image_files.extend(folder.rglob(f"*{ext.upper()}"))

    if not image_files:
        print("未找到任何支持的图片文件！")
        return []

    print(f"找到 {len(image_files)} 张图片，开始多线程计算哈希值...")

    # 使用线程池并行计算哈希
    hash_map = {}
    completed_count = 0
    total_files = len(image_files)

    # 锁用于打印进度，避免乱序（可选，这里简化处理，仅在完成后统计）
    with ThreadPoolExecutor(max_workers=MAX_WORKERS) as executor:
        # 提交所有任务
        future_to_path = {
            executor.submit(get_image_hashes, img_path): img_path
            for img_path in image_files
        }

        for future in as_completed(future_to_path):
            img_path, result = future.result()
            if result is not None:
                hash_map[img_path] = result

            completed_count += 1
            if completed_count % 10 == 0 or completed_count == total_files:
                print(f"处理进度: {completed_count}/{total_files}")

    if not hash_map:
        print("未能成功计算任何图片的哈希值！")
        return []

    # 两两比较查找重复图片
    print("\n开始比较图片相似度...")
    duplicates = []  # 存储重复组
    processed = set()

    paths = list(hash_map.keys())
    total_paths = len(paths)

    # 简单的两两比对，这部分主要是内存计算，通常比IO快，暂不并行化以避免复杂的锁竞争
    # 如果图片数量超过几千张，可以考虑更高级的索引结构（如KD-Tree），但对于一般场景，循环即可
    for i in range(total_paths):
        if paths[i] in processed:
            continue

        duplicate_group = [paths[i]]

        for j in range(i + 1, total_paths):
            if paths[j] in processed:
                continue

            # 计算综合哈希距离
            # imagehash 对象支持减法操作，返回汉明距离
            phash_dist = hash_map[paths[i]][0] - hash_map[paths[j]][0]
            dhash_dist = hash_map[paths[i]][1] - hash_map[paths[j]][1]

            # 加权平均距离
            avg_distance = PHASH_WEIGHT * phash_dist + DHASH_WEIGHT * dhash_dist

            # 如果距离小于阈值，认为是重复
            if avg_distance <= HASH_DISTANCE_THRESHOLD:
                duplicate_group.append(paths[j])
                processed.add(paths[j])

        # 如果该组有超过1张图片，记录为重复组
        if len(duplicate_group) > 1:
            processed.add(paths[i])
            duplicates.append(duplicate_group)

    return duplicates


def main():
    folder_path = FOLDER_PATH

    # 查找重复图片
    duplicate_groups = find_duplicates(folder_path)

    # 输出结果
    if not duplicate_groups:
        print("\n✅ 没有找到重复图片！")
        return

    print(f"\n🤖 共发现 {len(duplicate_groups)} 组重复图片：")
    print("=" * 80)

    for idx, group in enumerate(duplicate_groups, 1):
        print(f"\n【重复组 {idx}】包含 {len(group)} 张图片：")
        for i, img_path in enumerate(group, 1):
            try:
                file_size = os.path.getsize(img_path)
                size_mb = file_size / (1024 * 1024)
                # 再次打开图片获取尺寸，注意这里可能再次触发IO，如果性能敏感可缓存尺寸
                with Image.open(img_path) as img:
                    dimensions = img.size
                print(f"  {i}. {img_path}")
                print(f"     大小: {size_mb:.2f} MB | 尺寸: {dimensions}")
            except Exception as e:
                print(f"  {i}. {img_path} (读取详细信息失败: {e})")

    # 输出摘要信息
    print("\n" + "=" * 80)
    total_duplicates = sum(len(group) - 1 for group in duplicate_groups)
    print(
        f"总计：{len(duplicate_groups)} 组重复，共 {total_duplicates} 张可删除的重复图片"
    )

    # 保存结果到文件
    output_file = Path(folder_path) / "duplicate_report.txt"
    try:
        with open(output_file, "w", encoding="utf-8") as f:
            f.write(f"重复图片检测报告\n")
            f.write(f"检测时间：{Path(folder_path).stat().st_mtime}\n")
            f.write(f"阈值设置：汉明距离 ≤ {HASH_DISTANCE_THRESHOLD}\n")
            f.write(f"{'=' * 80}\n\n")

            for idx, group in enumerate(duplicate_groups, 1):
                f.write(f"重复组 {idx}：\n")
                for i, img_path in enumerate(group, 1):
                    f.write(f"  {i}. {img_path}\n")
                f.write("\n")
        print(f"详细报告已保存至：{output_file}")
    except Exception as e:
        print(f"保存报告失败: {e}")


if __name__ == "__main__":
    main()
