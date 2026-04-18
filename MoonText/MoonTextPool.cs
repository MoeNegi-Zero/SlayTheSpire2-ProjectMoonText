using Godot;
using MegaCrit.Sts2.Core.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoeNegiMod.MoonText
{
	public partial class MoonTextPool : Node2D
	{
		[Export] public int PoolSize = 1;  // 初始池大小

		private List<Control> pool = new List<Control>();

		public override void _Ready()
		{
			// 预创建节点，挂载 MoonText.gd 脚本
			for (int i = 0; i < PoolSize; i++)
			{
				var node = new Control();
				node.Name = $"MoonTextContainer_{i}";
				node.Visible = false;

				// 给节点挂载 MoonText.gd 脚本
				var script = PreloadManager.Cache.GetAsset<Script>("res://MoonText/MoonText.gd");
				node.SetScript(script);

				AddChild(node);
				pool.Add(node);
			}
		}

		/// <summary>
		/// 获取一个未使用节点
		/// </summary>
		public Control GetInstance()
		{
			foreach (var n in pool)
			{
				if (!n.Visible)
				{
					n.Visible = true;
					return n;
				}
			}

			// 池子用完，临时创建
			var newNode = new Control();
			newNode.Visible = true;
			newNode.Name = $"MoonTextContainer_{pool.Count}";
			var script = PreloadManager.Cache.GetAsset<Script>("res://MoonText/MoonText.gd");
			newNode.SetScript(script);
			AddChild(newNode);
			pool.Add(newNode);
			return newNode;
		}
	}
}
