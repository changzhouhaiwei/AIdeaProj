using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace FishTilePlay
{
    /// <summary>
    /// 表示游戏中棋子在多层关卡中的位置
    /// 包含X、Y坐标和层级ID，用于精确定位棋子位置
    /// </summary>
    [System.Serializable]
    public struct ElementPosition
    {
        /// <summary>
        /// 空位置常量，用于表示无效或未设置的位置
        /// </summary>
        public static readonly ElementPosition Empty = new ElementPosition(-1, -1, -1);

        /// <summary>
        /// 方向偏移量字典，用于快速计算相邻位置
        /// 定义了四个基本方向（上、下、左、右）的坐标偏移
        /// </summary>
        private static readonly Dictionary<Direction, ElementPosition> DIRECTION_OFFSET = new()
        {
            { Direction.Up, new ElementPosition(0, -1) },    // 向上移动：Y坐标减1
            { Direction.Down, new ElementPosition(0, 1) },   // 向下移动：Y坐标加1
            { Direction.Left, new ElementPosition(-1, 0) },  // 向左移动：X坐标减1
            { Direction.Right, new ElementPosition(1, 0) },  // 向右移动：X坐标加1
        };

        /// <summary>
        /// X坐标（水平位置）
        /// </summary>
        [SerializeField] int x;
        
        /// <summary>
        /// Y坐标（垂直位置）
        /// </summary>
        [SerializeField] int y;
        
        /// <summary>
        /// 层级ID（表示棋子所在的层级）
        /// </summary>
        [SerializeField] int layerId;

        /// <summary>
        /// X坐标属性
        /// </summary>
        public int X => x;
        
        /// <summary>
        /// Y坐标属性
        /// </summary>
        public int Y => y;
        
        /// <summary>
        /// 层级ID属性
        /// </summary>
        public int LayerId => layerId;

        /// <summary>
        /// 构造函数：创建一个新的位置
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="layerId">层级ID，默认为0（最顶层）</param>
        public ElementPosition(int x, int y, int layerId = 0)
        {
            this.x = x;
            this.y = y;
            this.layerId = layerId;
        }

        /// <summary>
        /// 复制构造函数：从另一个位置创建新位置
        /// </summary>
        /// <param name="position">要复制的位置</param>
        public ElementPosition(ElementPosition position)
        {
            x = position.x;
            y = position.y;
            layerId = position.layerId;
        }

        /// <summary>
        /// 构造函数：从另一个位置创建新位置，但指定新的层级ID
        /// </summary>
        /// <param name="position">要复制的位置</param>
        /// <param name="layerId">新的层级ID</param>
        public ElementPosition(ElementPosition position, int layerId)
        {
            x = position.x;
            y = position.y;
            this.layerId = layerId;
        }

        /// <summary>
        /// 位置加法运算符：将两个位置的坐标相加
        /// 注意：结果位置的层级ID使用第一个操作数的层级ID
        /// </summary>
        /// <param name="a">第一个位置</param>
        /// <param name="b">第二个位置</param>
        /// <returns>坐标相加后的新位置</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ElementPosition operator +(ElementPosition a, ElementPosition b) => new ElementPosition(a.X + b.X, a.Y + b.Y, a.LayerId);

        /// <summary>
        /// 比较两个位置是否相等（仅比较X和Y坐标，忽略层级ID）
        /// </summary>
        /// <param name="other">要比较的位置</param>
        /// <returns>如果X和Y坐标相同返回true，否则返回false</returns>
        public bool Equals(ElementPosition other)
        {
            return (x == other.x && y == other.y);
        }

        /// <summary>
        /// 比较当前位置是否等于指定的X、Y坐标
        /// </summary>
        /// <param name="x">要比较的X坐标</param>
        /// <param name="y">要比较的Y坐标</param>
        /// <returns>如果坐标相同返回true，否则返回false</returns>
        public bool Equals(int x, int y)
        {
            return (this.x == x && this.y == y);
        }

        /// <summary>
        /// 计算位置的哈希码
        /// 使用Cantor配对函数确保哈希码的唯一性和分布性
        /// </summary>
        /// <returns>位置的哈希码</returns>
        public override int GetHashCode()
        {
            return ((x + y) * (x + y + 1) / 2) + y;
        }

        /// <summary>
        /// 将位置转换为字符串表示
        /// </summary>
        /// <returns>格式化的位置字符串，如"(x:1, y:2, layerId:0)"</returns>
        public override string ToString()
        {
            return string.Format("(x:{0}, y:{1}, layerId:{2})", x, y, layerId);
        }

        /// <summary>
        /// 检查位置是否为空（无效位置）
        /// </summary>
        /// <returns>如果X、Y或LayerId为-1返回true，否则返回false</returns>
        public bool IsInvalidPos()
        {
            return x == -1 || y == -1 || layerId == -1;
        }

        /// <summary>
        /// 获取右侧相邻位置
        /// </summary>
        public ElementPosition RightNeighbourPos => this + DIRECTION_OFFSET[Direction.Right];
        
        /// <summary>
        /// 获取左侧相邻位置
        /// </summary>
        public ElementPosition LeftNeighbourPos => this + DIRECTION_OFFSET[Direction.Left];
        
        /// <summary>
        /// 获取上方相邻位置
        /// </summary>
        public ElementPosition UpNeighbourPos => this + DIRECTION_OFFSET[Direction.Up];
        
        /// <summary>
        /// 获取下方相邻位置
        /// </summary>
        public ElementPosition BottomNeighbourPos => this + DIRECTION_OFFSET[Direction.Down];

        /// <summary>
        /// 位置与Unity.Mathematics.int2的加法运算符
        /// 用于与Unity的数学库兼容
        /// </summary>
        /// <param name="first">位置</param>
        /// <param name="second">int2向量</param>
        /// <returns>坐标相加后的新位置</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ElementPosition operator +(ElementPosition first, int2 second) => new ElementPosition { x = first.x + second.x, y = first.y + second.y };

        /// <summary>
        /// 位置与Unity.Mathematics.int2的减法运算符
        /// 用于与Unity的数学库兼容
        /// </summary>
        /// <param name="first">位置</param>
        /// <param name="second">int2向量</param>
        /// <returns>坐标相减后的新位置</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ElementPosition operator -(ElementPosition first, int2 second) => new ElementPosition { x = first.x - second.x, y = first.y - second.y };
    }
}
