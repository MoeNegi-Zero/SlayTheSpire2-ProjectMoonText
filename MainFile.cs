using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace MoonText;

[ModInitializer(nameof(Initialize))]
public class MainFile
{
	public const string ModId = "MoonText";

	public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
		new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

	public static void Initialize()
	{
		Harmony harmony = new(ModId);
		
		harmony.PatchAll();
	}
}
