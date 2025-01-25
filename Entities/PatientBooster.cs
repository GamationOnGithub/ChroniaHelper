﻿using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections;
using System.Data.Common;
using System.Reflection;
using ChroniaHelper.Utils;

namespace ChroniaHelper.Entities;

[CustomEntity("ChroniaHelper/PatientBooster")]
public class PatientBooster : Booster
{
	public static Booster TempCurrentBooster = null;

	private float respawnDelay;
	private int? refillDashes;
	private bool refillStamina;

	private Vector2? lastSpritePos;

	public Sprite Sprite => sprite;

	public PatientBooster(EntityData data, Vector2 offset)
		: base(data.Position + offset, data.Bool("red"))
	{
		// parsing hitbox data
		string[] hitboxData = data.Attr("customHitbox").Split(';',StringSplitOptions.TrimEntries);
        //对于每组数据
        ColliderList CL = new ColliderList();
        for (int i = 0; i < hitboxData.Length; i++)
        {
            //首先分割并去空
            string[] hb = hitboxData[i].Split(",", StringSplitOptions.TrimEntries);
            //淘汰length为0
            if (hb.Length == 0) { break; }
            //length !=0 , 检查第一位
            if (hb[0] == "" || (hb[0] != "c" && hb[0] != "r")) { break; }
            //第一位稳定，开始记录
            float p1 = 0f, p2 = 0f, p3 = 0f, p4 = 0f;
            if (hb.Length >= 2)
            {
                p1 = hb[1].ParseFloat(0f);
            }
            if (hb.Length >= 3)
            {
                p2 = hb[2].ParseFloat(0f);
            }
            if (hb.Length >= 4)
            {
                p3 = hb[3].ParseFloat(0f);
            }
            if (hb.Length >= 5)
            {
                p4 = hb[4].ParseFloat(0f);
            }
            if (hb[0] == "r")
            {
                if (p1 <= 0) { break; }
                if (p2 <= 0) { break; }
                CL.Add(new Hitbox(p1, p2, p3, p4));
            }
            else
            {
                if (p1 <= 0) { break; }
                CL.Add(new Circle(p1, p2, p3));
            }
        }
        base.Collider = CL;
        
		respawnDelay = data.Float("respawnDelay", 1f);
		refillDashes = Utils.NumberUtils.OptionalInt(data, "refillDashes", null);
		refillStamina = data.Bool("refillStamina", true);

		var spriteName = data.Attr("sprite", "");
		var red = data.Bool("red");

		Remove(sprite);
		Add(sprite = GFX.SpriteBank.Create(!string.IsNullOrEmpty(spriteName) ? spriteName : (red ? "Preset_red" : "Preset_green")));
	}

	public override void Update()
	{
		base.Update();
		var player = Scene.Tracker.GetEntity<Player>();
		if (player != null && player.CurrentBooster == this)
		{
			BoostingPlayer = true;
			player.boostTarget = Center;
			var targetPos = Center - player.Collider.Center + (Input.Aim.Value * 3f);
			player.MoveToX(targetPos.X);
			player.MoveToY(targetPos.Y);
		}
		var sprite = Sprite;
		if (sprite.CurrentAnimationID == "pop")
		{
			if (lastSpritePos == null)
			{
				lastSpritePos = sprite.RenderPosition;
			}

			sprite.RenderPosition = lastSpritePos.Value;
		}
		else
		{
			lastSpritePos = null;
		}
	}

	private static ILHook origBoostBeginHook;

	public static void Load()
	{
		On.Celeste.Player.Boost += Player_Boost;
		On.Celeste.Player.RedBoost += Player_RedBoost;
		On.Celeste.Player.BoostCoroutine += Player_BoostCoroutine;
		On.Celeste.Booster.PlayerReleased += Booster_PlayerReleased;
		IL.Celeste.Player.BoostBegin += Player_BoostBegin;

		origBoostBeginHook = new ILHook(typeof(Player).GetMethod("orig_BoostBegin", BindingFlags.NonPublic | BindingFlags.Instance), Player_BoostBegin);
	}

	public static void Unload()
	{
		On.Celeste.Player.Boost -= Player_Boost;
		On.Celeste.Player.RedBoost -= Player_RedBoost;
		On.Celeste.Player.BoostCoroutine -= Player_BoostCoroutine;
		On.Celeste.Booster.PlayerReleased -= Booster_PlayerReleased;
		IL.Celeste.Player.BoostBegin -= Player_BoostBegin;

		origBoostBeginHook?.Dispose();
		origBoostBeginHook = null;
	}

	private static void Player_Boost(On.Celeste.Player.orig_Boost orig, Player self, Booster booster)
	{
		TempCurrentBooster = booster;
		orig(self, booster);
		TempCurrentBooster = null;
	}

	private static void Player_RedBoost(On.Celeste.Player.orig_RedBoost orig, Player self, Booster booster)
	{
		TempCurrentBooster = booster;
		orig(self, booster);
		TempCurrentBooster = null;
	}

	private static IEnumerator Player_BoostCoroutine(On.Celeste.Player.orig_BoostCoroutine orig, Player self)
	{
		if (self.CurrentBooster is PatientBooster)
		{
			yield break;
		}

		var orig_enum = orig(self);
		while (orig_enum.MoveNext())
		{
			yield return orig_enum.Current;
		}
	}

	private static void Booster_PlayerReleased(On.Celeste.Booster.orig_PlayerReleased orig, Booster self)
	{
		orig(self);

		if (self is PatientBooster patientBooster)
		{
			patientBooster.respawnTimer = patientBooster.respawnDelay;
		}
	}

	private static void Player_BoostBegin(ILContext il)
	{
		var cursor = new ILCursor(il);

		if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<Player>("RefillStamina")))
		{
			Logger.Log(LogLevel.Error, "ChroniaHelper", $"Failed IL Hook for Booster.BoostBegin (RefillStamina)");
			return;
		}

		var afterRefillsLabel = cursor.MarkLabel();

		if (!cursor.TryGotoPrev(MoveType.AfterLabel, instr => instr.MatchCallvirt<Player>("RefillDash")))
		{
			Logger.Log(LogLevel.Error, "ChroniaHelper", $"Failed IL Hook for Booster.BoostBegin (RefillDash)");
			return;
		}

		var continueLabel = cursor.DefineLabel();

		cursor.Emit(OpCodes.Ldarg_0);
		cursor.EmitDelegate<Func<Player, bool>>(player =>
		{
			if (TempCurrentBooster is PatientBooster booster)
			{
				if (booster.refillDashes.HasValue)
				{
					player.Dashes = Math.Max(player.Dashes, booster.refillDashes.Value);
				}
				else
				{
					player.RefillDash();
				}

				if (booster.refillStamina)
				{
					player.RefillStamina();
				}

				return true;
			}
			return false;
		});
		cursor.Emit(OpCodes.Brfalse, continueLabel);
		cursor.Emit(OpCodes.Pop);
		cursor.Emit(OpCodes.Br, afterRefillsLabel);
		cursor.MarkLabel(continueLabel);
	}
}
