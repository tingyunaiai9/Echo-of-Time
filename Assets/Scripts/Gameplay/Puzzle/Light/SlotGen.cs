using System;
using System.Collections.Generic;
using System.Linq;

/*
 * 激光谜题生成器
 * 用于生成具有唯一解的光线反射谜题
 * 
 * 使用示例:
 * ```
 * LaserPuzzleGenerator generator = new LaserPuzzleGenerator(rows: 5, cols: 11);
 * var (allSlots, start, end, solutionSlots) = generator.Generate();
 * 
 * // allSlots: 包含所有镜槽的 MirrorSlot 数组（正确镜槽 + 干扰镜槽）
 * // start: 光线起点坐标 (行, 列)
 * // end: 光线终点坐标 (行, 列)
 * // solutionSlots: 正确解的 MirrorSlot 数组（只包含需要激活的镜槽）
 * 
 * // 将生成的镜槽应用到 LightPanel
 * LightPanel lightPanel = GetComponent<LightPanel>();
 * lightPanel.mirrorSlots = allSlots;
 * ```
 * 
 * MirrorSlot 结构说明:
 * - xindex: 列索引 (0-10，对应网格列)
 * - yindex: 行索引 (0-4，对应网格行)
 * - direction: 镜子方向枚举
 *   - TOP_LEFT (45°): 光线从左/下进入，向上/右反射
 *   - BOTTOM_LEFT (135°): 光线从左/上进入，向下/右反射
 *   - BOTTOM_RIGHT (-135°): 光线从右/上进入，向下/左反射
 *   - TOP_RIGHT (-45°): 光线从右/下进入，向上/左反射
 */
public class LaserPuzzleGenerator
{
    private int rows;
    private int cols;
    private Dictionary<(int, int), char> grid; // 存储槽位信息 (r, c): type
    private Dictionary<(int, int), ((int, int), (int, int))> mirrorDirections; // 存储每个镜子的输入输出方向
    private (int, int) start;
    private (int, int) end;
    private (int, int) startDir;
    private int mirrorCount;
    private int distractorCount;

    public LaserPuzzleGenerator(int rows = 5, int cols = 11)
    {
        this.rows = rows;
        this.cols = cols;
        this.grid = new Dictionary<(int, int), char>();
        this.mirrorDirections = new Dictionary<(int, int), ((int, int), (int, int))>();
        this.start = (0, 0);
        this.end = (0, 0);
        this.startDir = (0, 1);
        this.mirrorCount = 5;
        this.distractorCount = 3;
    }

    public (MirrorSlot[], (int, int), (int, int), MirrorSlot[]) Generate()
    {
        System.Random random = new System.Random();

        while (true)
        {
            // 1. 重置
            grid.Clear();
            mirrorDirections.Clear();

            // 2. 随机选择起点和初始方向 (假设从左边缘进入)
            int startRow = random.Next(0, rows);
            start = (startRow, -1); // 网格左侧外
            startDir = (0, 1); // 向右

            // 3. 生成一条合法的"正确路径"
            if (!CreateValidPath(random))
            {
                continue; // 路径生成失败，重试
            }

            // 4. 添加干扰项
            AddDistractors(random);

            // 5. 验证唯一解
            var solutions = SolvePuzzle();
            if (solutions.Count == 1)
            {
                // 找到了完美的谜题
                MirrorSlot[] allSlots = ConvertToMirrorSlots(grid);
                MirrorSlot[] solutionSlots = ConvertToMirrorSlots(solutions[0]);
                return (allSlots, start, end, solutionSlots);
            }

            // 如果解不唯一或无解（理论上不会无解因为是逆向生成的），重试
        }
    }

    private bool CreateValidPath(System.Random random)
    {
        var curr = (start.Item1 + startDir.Item1, start.Item2 + startDir.Item2);
        var direction = startDir;
        List<(int, int)> pathMirrors = new List<(int, int)>();

        for (int i = 0; i < mirrorCount; i++)
        {
            List<(int, int, int)> candidates = new List<(int, int, int)>();
            int r = curr.Item1, c = curr.Item2, dist = 0;

            while (r >= 0 && r < rows && c >= 0 && c < cols)
            {
                if (!grid.ContainsKey((r, c)))
                {
                    candidates.Add((r, c, dist));
                }
                r += direction.Item1;
                c += direction.Item2;
                dist++;
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            var choice = candidates[random.Next(candidates.Count)];
            curr = (choice.Item1, choice.Item2);

            var nextDirs = direction.Item1 == 0
                ? new List<(int, int)> { (-1, 0), (1, 0) }
                : new List<(int, int)> { (0, -1), (0, 1) };

            var nextDir = nextDirs[random.Next(nextDirs.Count)];
            char mirrorType = GetMirrorType(direction, nextDir);

            grid[curr] = mirrorType;
            mirrorDirections[curr] = (direction, nextDir); // 存储输入输出方向
            pathMirrors.Add(curr);
            direction = nextDir;

            curr = (curr.Item1 + direction.Item1, curr.Item2 + direction.Item2);
        }

        int rEnd = curr.Item1, cEnd = curr.Item2;
        while (rEnd >= 0 && rEnd < rows && cEnd >= 0 && cEnd < cols)
        {
            if (grid.ContainsKey((rEnd, cEnd))) return false;
            rEnd += direction.Item1;
            cEnd += direction.Item2;
        }

        end = (rEnd, cEnd);
        return true;
    }

    private char GetMirrorType((int, int) dIn, (int, int) dOut)
    {
        if (dIn.Item1 == 0)
        {
            return (dIn.Item2 == 1 && dOut.Item1 == -1) || (dIn.Item2 == -1 && dOut.Item1 == 1) ? '/' : '\\';
        }
        else
        {
            return (dIn.Item1 == 1 && dOut.Item2 == -1) || (dIn.Item1 == -1 && dOut.Item2 == 1) ? '/' : '\\';
        }
    }

    private void AddDistractors(System.Random random)
    {
        int count = 0;
        while (count < distractorCount)
        {
            int r = random.Next(0, rows);
            int c = random.Next(0, cols);
            if (!grid.ContainsKey((r, c)))
            {
                char mirrorType = random.Next(2) == 0 ? '/' : '\\';
                grid[(r, c)] = mirrorType;
                // 为干扰项添加默认方向（因为不参与正确路径，方向信息用于显示）
                mirrorDirections[(r, c)] = ((0, 1), (1, 0)); // 默认方向
                count++;
            }
        }
    }

    private List<Dictionary<(int, int), char>> SolvePuzzle()
    {
        var slots = grid.Keys.ToList();
        var allSlots = grid.ToList();
        List<Dictionary<(int, int), char>> validSolutions = new List<Dictionary<(int, int), char>>();

        foreach (var combination in GetCombinations(allSlots, mirrorCount))
        {
            var activeMirrors = combination.ToDictionary(x => x.Key, x => x.Value);
            if (SimulateLight(activeMirrors))
            {
                validSolutions.Add(activeMirrors);
            }
        }

        return validSolutions;
    }

    private bool SimulateLight(Dictionary<(int, int), char> activeMirrors)
    {
        int r = start.Item1 + startDir.Item1;
        int c = start.Item2 + startDir.Item2;
        int dr = startDir.Item1, dc = startDir.Item2;
        int steps = 0, hitMirrors = 0;

        while (r >= 0 && r < rows && c >= 0 && c < cols)
        {
            steps++;
            if (steps > 100) return false;

            if (activeMirrors.ContainsKey((r, c)))
            {
                hitMirrors++;
                char mtype = activeMirrors[(r, c)];
                if (mtype == '/')
                {
                    (dr, dc) = (-dc, -dr);
                }
                else
                {
                    (dr, dc) = (dc, dr);
                }
            }

            r += dr;
            c += dc;
        }

        return (r, c) == end && hitMirrors == mirrorCount;
    }

    private IEnumerable<List<KeyValuePair<(int, int), char>>> GetCombinations(
        List<KeyValuePair<(int, int), char>> list, int length)
    {
        if (length == 0) yield return new List<KeyValuePair<(int, int), char>>();
        else
        {
            for (int i = 0; i < list.Count; i++)
            {
                foreach (var tail in GetCombinations(list.Skip(i + 1).ToList(), length - 1))
                {
                    yield return new List<KeyValuePair<(int, int), char>> { list[i] }.Concat(tail).ToList();
                }
            }
        }
    }

    /*
     * 将字符类型镜子转换为 MirrorSlot.Direction 枚举
     * '/' 和 '\\' 根据其在光线路径中的作用映射到四个方向
     */
    private MirrorSlot.Direction CharToDirection(char mirrorChar, (int, int) position)
    {
        // 根据镜子字符和位置特征确定方向
        // '/' 镜子: 光线从左/下来时反射到上/右
        // '\\' 镜子: 光线从左/上来时反射到下/右
        
        if (mirrorChar == '/')
        {
            // '/' 对应 TOP_LEFT (45°) 或 BOTTOM_RIGHT (-135°)
            // 这里简化处理，根据行号奇偶性分配
            return (position.Item1 % 2 == 0) ? MirrorSlot.Direction.TOP_LEFT : MirrorSlot.Direction.BOTTOM_RIGHT;
        }
        else // mirrorChar == '\\'
        {
            // '\\' 对应 BOTTOM_LEFT (135°) 或 TOP_RIGHT (-45°)
            return (position.Item1 % 2 == 0) ? MirrorSlot.Direction.BOTTOM_LEFT : MirrorSlot.Direction.TOP_RIGHT;
        }
    }

    /*
     * 将网格字典转换为 MirrorSlot 数组
     */
    private MirrorSlot[] ConvertToMirrorSlots(Dictionary<(int, int), char> gridData)
    {
        List<MirrorSlot> slots = new List<MirrorSlot>();
        
        foreach (var kvp in gridData)
        {
            var position = kvp.Key;
            MirrorSlot.Direction direction;
            
            // 如果有方向信息，使用精确映射
            if (mirrorDirections.ContainsKey(position))
            {
                var (dIn, dOut) = mirrorDirections[position];
                direction = GetMirrorDirection(dIn, dOut);
            }
            else
            {
                // 否则使用简化映射（用于干扰项）
                direction = CharToDirection(kvp.Value, position);
            }
            
            MirrorSlot slot = new MirrorSlot
            {
                xindex = position.Item2,  // 列索引
                yindex = position.Item1,  // 行索引
                direction = direction
            };
            slots.Add(slot);
        }
        
        return slots.ToArray();
    }

    /*
     * 根据输入输出方向获取镜子方向枚举
     * 更精确地映射光线反射方向到 MirrorSlot.Direction
     */
    private MirrorSlot.Direction GetMirrorDirection((int, int) dIn, (int, int) dOut)
    {
        // dIn: 输入方向, dOut: 输出方向
        // TOP_LEFT (45°): \\ 向上左反射
        // BOTTOM_LEFT (135°): / 向下左反射  
        // BOTTOM_RIGHT (-135°): \\ 向下右反射
        // TOP_RIGHT (-45°): / 向上右反射
        
        if (dIn.Item1 == 0 && dIn.Item2 == 1) // 从左向右
        {
            if (dOut.Item1 == -1 && dOut.Item2 == 0) // 向上反射
                return MirrorSlot.Direction.TOP_RIGHT; // '/' 镜子，-45°
            else // 向下反射
                return MirrorSlot.Direction.BOTTOM_LEFT; // '\\' 镜子，135°
        }
        else if (dIn.Item1 == 0 && dIn.Item2 == -1) // 从右向左
        {
            if (dOut.Item1 == -1 && dOut.Item2 == 0) // 向上反射
                return MirrorSlot.Direction.TOP_LEFT; // '\\' 镜子，45°
            else // 向下反射
                return MirrorSlot.Direction.BOTTOM_RIGHT; // '/' 镜子，-135°
        }
        else if (dIn.Item1 == 1 && dIn.Item2 == 0) // 从上向下
        {
            if (dOut.Item1 == 0 && dOut.Item2 == -1) // 向左反射
                return MirrorSlot.Direction.BOTTOM_RIGHT; // '/' 镜子，-135°
            else // 向右反射
                return MirrorSlot.Direction.BOTTOM_LEFT; // '\\' 镜子，135°
        }
        else // 从下向上 (dIn.Item1 == -1 && dIn.Item2 == 0)
        {
            if (dOut.Item1 == 0 && dOut.Item2 == -1) // 向左反射
                return MirrorSlot.Direction.TOP_LEFT; // '\\' 镜子，45°
            else // 向右反射
                return MirrorSlot.Direction.TOP_RIGHT; // '/' 镜子，-45°
        }
    }

    public static void Main(string[] args)
    {
        // 创建一个激光谜题生成器实例
        LaserPuzzleGenerator generator = new LaserPuzzleGenerator(rows: 5, cols: 11);

        // 生成谜题
        var (allSlots, start, end, solutionSlots) = generator.Generate();

        // 输出起点和终点
        Console.WriteLine($"起点: ({start.Item1}, {start.Item2})");
        Console.WriteLine($"终点: ({end.Item1}, {end.Item2})");

        // 输出所有镜槽信息
        Console.WriteLine("所有镜槽:");
        foreach (var slot in allSlots)
        {
            Console.WriteLine($"位置: ({slot.yindex}, {slot.xindex}), 方向: {slot.direction}");
        }

        // 输出正确解的镜槽信息
        Console.WriteLine("正确解:");
        foreach (var slot in solutionSlots)
        {
            Console.WriteLine($"位置: ({slot.yindex}, {slot.xindex}), 方向: {slot.direction}");
        }
    }
}