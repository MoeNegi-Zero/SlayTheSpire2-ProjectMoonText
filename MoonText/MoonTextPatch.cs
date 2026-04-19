using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MoeNegiMod.MoonText;
using System.IO;
using System.Linq;
using System.Text.Json;
using Timer = Godot.Timer;

[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
public static class MoonTextPatch
{
	private static MoonTextPool pool;

	public static MoonTextData[] moonTextArray;


	public static void Postfix(NCombatRoom __instance)
	{

		if (moonTextArray == null)
		{
			string exeDir = Path.GetDirectoryName(OS.GetExecutablePath());
			string jsonPath = Path.Combine(exeDir, "Mods", "MoonText", "MoonTextOption.json");

			if (!Directory.Exists(jsonPath))
			{
				jsonPath = Path.Combine(exeDir, "mods", "MoonText", "MoonTextOption.json");
			}
			if (!File.Exists(jsonPath))
			{
				Log.Warn($"Cannot find JSON: {jsonPath}", 2);
				moonTextArray = new MoonTextData[]
				{
					new MoonTextData { text = "请检查(Mods/mods)/MoonText/MoonTextOption.json", time = 5f }
				};
			}
			else
			{
				moonTextArray = JsonSerializer.Deserialize<MoonTextData[]>(File.ReadAllText(jsonPath));
			}
		}

		pool = __instance.GetNodeOrNull<MoonTextPool>("MoonTextPool");
		if (pool == null)
		{
			// 创建并挂载
			pool = new MoonTextPool();
			pool.Name = "MoonTextPool";
			__instance.AddChild(pool);
			pool._Ready(); // 初始化池
		}

		bool hasTimeValue = moonTextArray.All(d => d.time.HasValue);
		bool noneTimeValue = moonTextArray.All(d => !d.time.HasValue);

		if (hasTimeValue)
		{
			StartSequentialSpawn(__instance);
		}
		else if (noneTimeValue)
		{
			StartRandomSpawn(__instance);
		}
		else
		{
			Log.Warn("[>>>MoonTextMod] JSON 内容必须全部有 time 或全部缺省", 2);
		}
	}

	public static void StartRandomSpawn(NCombatRoom __instance)
	{
		if (moonTextArray == null || moonTextArray.Length == 0) return;

		Timer spawnTimer = new Timer();

		spawnTimer.WaitTime = 5f;
		spawnTimer.OneShot = false;  // 循环触发
		spawnTimer.Autostart = true; // 自动启动
		__instance.AddChild(spawnTimer);

		spawnTimer.Timeout += () =>
		{
			float x = (float)GD.RandRange(150.0, 1300.0);
			float y = (float)GD.RandRange(200.0, 650.0);
			Log.Info("[>>>MoonTextMod Timeout]");
			SpawnText(moonTextArray[(int)(GD.Randi() % moonTextArray.Length)].text, new Vector2(x, y));
		};
	}

	public static void StartSequentialSpawn(NCombatRoom __instance)
	{
		if (moonTextArray == null || moonTextArray.Length == 0) return;

		int index = 0;
		Timer sequentialTimer = new Timer();
		sequentialTimer.OneShot = true;
		__instance.AddChild(sequentialTimer);

		void ScheduleNext()
		{
			if (index >= moonTextArray.Length) return;

			float currentTime = (float)moonTextArray[index].time;

			// 1️⃣ 找出当前时间点的所有文字
			int batchStart = index;
			int batchEnd = index;

			while (batchEnd < moonTextArray.Length && moonTextArray[batchEnd].time == currentTime)
			{
				batchEnd++;
			}

			// 2️⃣ 计算等待时间（看下一条不同时间的文字）
			float waitTime;
			if (batchEnd < moonTextArray.Length)
			{
				float nextTime = (float)moonTextArray[batchEnd].time;
				waitTime = nextTime - currentTime;
			}
			else
			{
				waitTime = 5f; // 默认等待时间
			}
			if (waitTime > 10f) waitTime=5f;

			// 3️⃣ 计算动画参数
			float typingDuration = waitTime / 5f;
			float floatDelay = waitTime * 4f / 5f;
			float floatTime = 2f;

			// 4️⃣ 批量生成文字
			for (int i = batchStart; i < batchEnd; i++)
			{
				var data = moonTextArray[i];
				float x = (float)GD.RandRange(0.0, 1350.0);
				float y = (float)GD.RandRange(200.0, 650.0);

				SpawnText(data.text, new Vector2(x, y),
						  typingDuration, floatDelay, floatTime);
			}

			index = batchEnd;

			// 5️⃣ 设置下一次 Timer
			if (index < moonTextArray.Length)
			{
				sequentialTimer.WaitTime = (float)(moonTextArray[index].time - currentTime);
				sequentialTimer.Start();
			}
		}

		// 第一次触发时间
		sequentialTimer.WaitTime = (float)moonTextArray[0].time;
		sequentialTimer.Timeout += ScheduleNext;
		sequentialTimer.Start();
	}

	private static void SpawnText(string Text, Vector2 pos)
	{
		var node = pool.GetInstance();
		Log.Info("[>>>MoonTextMod SpwaningMoonText,Text=]" + Text + "pos=" + pos);
		node.Set("text",Text);
		/*node.Set("font_size", 100);
		node.Set("float_time", 5.0);*/
		node.Position = pos;
		node.Call("spawn");
	}

	private static void SpawnText(string text, Vector2 pos, float typingDuration, float floatDeley, float floatTime)
	{
		string plaintText = System.Text.RegularExpressions.Regex.Replace(text, @"\[(\/?[^\]]+)\]", "");

		float charDelay = typingDuration / plaintText.Length;

		var node = pool.GetInstance();
		Log.Info("[>>>MoonTextMod SpwaningMoonText,Text=]" + text + "pos=" + pos);
		node.Set("text", text);
		node.Set("char_delay", charDelay);
		node.Set("float_delay", floatDeley);
		node.Set("float_time", floatTime);
		node.Position = pos;
		node.Call("spawn");
	}

}
