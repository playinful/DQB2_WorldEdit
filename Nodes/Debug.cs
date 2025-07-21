/*using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.ExceptionServices;

namespace EyeOfRubiss.Testing.Nodes
{
	public partial class Debug : PopupMenu
	{
		public void _MenuButtonPressed(int id)
		{
			switch (id)
			{
				case 0:
					if (CommonData.Instance is not null)
					{
						byte voiceType = 0;
						foreach (CommonData.Resident resident in CommonData.Instance.Residents)
						{
							if (resident.CurrentIsland == 12)
							{
								resident.Name = $"VoiceType {voiceType}";
								resident.VoiceType = voiceType;
								voiceType++;
							}
						}
					}
					break;
				case 3:
					if (CommonData.Instance is not null)
					{
						byte messageType = 0;
						foreach (CommonData.Resident resident in CommonData.Instance.Residents)
						{
							if (resident.CurrentIsland == 12)
							{
								resident.Name = $"MsgType {messageType}";
								resident.MessageType = messageType;
								messageType++;
							}
						}
					}
					break;
				case 4:
					if (CommonData.Instance is not null)
					{
						byte genericName = 0;
						foreach (CommonData.Resident resident in CommonData.Instance.Residents)
						{
							if (resident.CurrentIsland == 12)
							{
								resident.UseCustomName = false;
								resident.GenericName = genericName;
								resident.Sex = 0b101;
								genericName++;
							}
						}
					}
					break;
				case 5:
					if (CommonData.Instance is not null)
					{
						for (int i = 0; i < 420; i++)
						{
							CommonData.Instance.BagInventory[i].ItemID = (ushort)(i + 420 * 9);
							CommonData.Instance.BagInventory[i].Count = (short)(i + 420 * 9);
						}
					}
					break;
				case 6:
					if (CommonData.Instance is not null)
					{
						int[] things = new int[] {1933,1763,251,2293,1764,1740,2384,1318,2563,2386,2387,2388,621,620,2283,622,2389,3853,3854,3855,3856,3857,3858,3859,3860,1420,2455,1319,1320,2380,2353,2354,2391,2537,2538,2549,2555,2578,418,2292,2308,2309,2310,2311,2313,69,2304,2305,2306,2542,2412,2413,2414,2307,2316,416,423,435,412,2457,2456,2459,2460,247,248,266,918,926,934,919,927,935,920,928,936,921,929,937,922,930,938,923,931,939,924,932,940,925,933,941,675,255,256,1271,3177,3357,1508,3176,3356,2721,3358,2722,3359,2723,3360,2724,3361,2725,3362,2726,3363,2727,3364,2728,3365,3941,3976,3549,3977,3659,3990,3660,3991,3661,3992,3662,3993,3663,3994,3664,3995,3665,3996,3666,3997,1404,1487,2033,2047,2186,2194,2195,2220,2248,2265,1488,1457,1458,1459,1628,1629,1633,1634,2004,1269,2937,2938,2939,2940,2941,2942,2943,2944,1270,3079,3080,3081,3082,3083,3084,3085,3086,2039,3967,3968,3969,3970,3971,3972,3973,3974,3382,3457,3458,3459,3460,3461,3462,1507,3463,3464,1272,3095,3096,3097,3098,3099,3100,3101,3102,1885,3793,2392,2583,2584,2589,2590,2591,2592,2593,2594,2385,3388,3489,3490,3491,3492,3493,3494,3495,3496,267,2657,2658,2659,2660,2661,2662,2663,2664,1273,3087,3088,3089,3090,3091,3092,3093,3094,235,2267,2269,2282,2290,2320,2321,2327,2339,1258,2665,2666,2667,2668,2669,1506,2670,2671,2672,220,2673,2674,2675,2676,2677,2678,2679,2680,1983,2681,2682,2683,2684,2685,2686,2687,2688,679,2689,2690,2691,2692,2693,2694,2695,2696,2203,1489,1490,2572,642,2294,641,643,1317,1330,2458,1127,1505,1128,695,2697,2698,2699,2700,2701,2702,2703,2704,697,698,2079,2038,12,3744,14,3745,15,3746,18,3747,19,3748,21,3749,22,3750,23,3751,295,2406,3869,3870,3871,3872,3873,3874,3875,3876,2080,713,1169,1183,1184,1185,1504,1371,1372,1381,1415,3217,1266,745,746,747,754,755,758,759,767,1267,768,784,785,786,787,883,895,896,1268,2250,2190,2373,2303,1409,2641,2642,2643,2644,2645,2646,2647,2648,2198,2649,2650,2651,2652,2653,2654,2655,2656,1159,30,160,161,230,283,284,417,583,1503,2397,3603,3604,3605,3606,3607,3608,3609,3610,897,992,993,994,995,1798,1803,1804,1806,1925};
						for (int i = 0; i < things.Length; i++)
						{
							CommonData.Instance.BagInventory[i].ItemID = (ushort)things[i];
							CommonData.Instance.BagInventory[i].Count = 1;
						}
					}
					break;
				case 1:
					if (CommonData.Instance is not null)
					{
						List<byte> UsedVoices = new();
						foreach (CommonData.Resident resident in CommonData.Instance.ImportantResidents)
						{
							if (!UsedVoices.Contains(resident.VoiceType))
							{
								UsedVoices.Add(resident.VoiceType);
							}
						}
						foreach (CommonData.Resident resident in CommonData.Instance.Residents)
						{
							if (!resident.Name.StartsWith("VoiceType") && !UsedVoices.Contains(resident.VoiceType))
							{
								UsedVoices.Add(resident.VoiceType);
							}
						}
						UsedVoices.Sort();
						foreach(byte voice in UsedVoices)
						{
							GD.Print(voice);
						}
					}
					break;
				case 2:
					if (CommonData.Instance is not null)
					{
						foreach (CommonData.Resident resident in CommonData.Instance.ImportantResidents)
						{
							GD.Print($"Resident {resident.Index} @ 0x{resident.GetAddress():X8} : {resident.GetDisplayName()}");
						}
						foreach (CommonData.Resident resident in CommonData.Instance.Residents)
						{
							GD.Print($"Resident {resident.Index} @ 0x{resident.GetAddress():X8} : {resident.GetDisplayName()}");
						}
					}
					break;
				default:
					throw new ArgumentException($"Unsupported ID {id}");
			}
		}
	}
}
*/