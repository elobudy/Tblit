﻿using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Color = System.Drawing.Color;

namespace VolatileAIO.Organs.Brain
{
    internal class DrawManager : Heart
    {
        private bool _initialized;
        private Spell.SpellBase _q, _w, _e, _r;

        
        internal void UpdateValues(Spell.SpellBase q, Spell.SpellBase w, Spell.SpellBase e, Spell.SpellBase r)
        {
            if (!TickManager.NoLag(0)) return;
            if (!_initialized) _initialized = true;
            _q = q;
            _w = w;
            _e = e;
            _r = r;
        }

        private void DrawDamageIndicator()
        {
            if (!_initialized) return;
            var target = TargetManager.Target(1000, DamageType.Physical);
            if (target != null)
            {
                //Drawing.DrawCircle(target.Position, 100, Color.Red);
                Circle.Draw(new ColorBGRA(Color.Red.R, Color.Red.G, Color.Red.B, Color.Red.A), 20, 3F, target.Position);
            }

            const int width = 103;
            const int height = 9;
            const int xOffset = -11;
            const int yOffset = 17;
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                if (!enemy.IsHPBarRendered) continue;
                var barPosition = enemy.HPBarPosition;
                float drawDamage = 0, drawQ = 0, drawW = 0, drawE = 0, drawR = 0;

                if (_q.IsReady()) drawDamage += Player.GetSpellDamage(enemy, SpellSlot.Q);
                if (_w.IsReady()) drawDamage += Player.GetSpellDamage(enemy, SpellSlot.W);
                if (_e.IsReady()) drawDamage += Player.GetSpellDamage(enemy, SpellSlot.E);
                if (_r.IsReady()) drawDamage += Player.GetSpellDamage(enemy, SpellSlot.R);

                if (_q.IsReady()) drawQ = (Player.GetSpellDamage(enemy, SpellSlot.Q) / drawDamage);
                if (_w.IsReady()) drawW = (Player.GetSpellDamage(enemy, SpellSlot.W) / drawDamage);
                if (_e.IsReady()) drawE = (Player.GetSpellDamage(enemy, SpellSlot.E) / drawDamage);
                if (_r.IsReady()) drawR = (Player.GetSpellDamage(enemy, SpellSlot.R) / drawDamage);

                var hpleft = Math.Max(0, enemy.Health - drawDamage) / enemy.MaxHealth;
                var yPos = barPosition.Y + yOffset;
                var xPosDamage = barPosition.X + xOffset + width * hpleft;
                var xPosCurrentHp = barPosition.X + xOffset + width * enemy.Health / enemy.MaxHealth;
                var differenceInHp = xPosCurrentHp - xPosDamage;
                var pos1 = barPosition.X + xOffset + (107 * hpleft);
                for (var i = 0; i < differenceInHp; i++)
                {
                    if (_q.IsReady() && i < drawQ * differenceInHp)
                        Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + height, 1, Color.Cyan);
                    else if (_w.IsReady() && i < (drawQ + drawW) * differenceInHp)
                        Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + height, 1, Color.Orange);
                    else if (_e.IsReady() && i < (drawQ + drawW + drawE) * differenceInHp)
                        Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + height, 1, Color.Yellow);
                    else if (_r.IsReady() && i < (drawQ + drawW + drawE + drawR) * differenceInHp)
                        Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + height, 1, Color.YellowGreen);
                }
            }
        }

        private void DrawDebug()
        {
            //CursorPos
            Drawing.DrawText(Game.CursorPos2D.X, Game.CursorPos2D.Y - 20, Color.Red, Game.CursorPos2D.X + "," + Game.CursorPos2D.Y);
            
            //Q Hitchance as seen in L$'s SPred
            var w = Drawing.Width - 200;
            var h = (Drawing.Height / 3) * 1.5;
            float hitchance = 0;
            if (CastManager.CastCount != 0 && CastManager.HitCount != 0)
                hitchance = (CastManager.HitCount / CastManager.CastCount) * 100;
            Drawing.DrawText(w, (float)h, Color.Red,
                String.Format("Casted Spell.Q Count: {0}", CastManager.CastCount));
            Drawing.DrawText(w, (float)h + 20, Color.Red,
                String.Format("Hit Spell.Q Count: {0}", CastManager.HitCount));
            Drawing.DrawText(w, (float)h + 40, Color.Red,
                String.Format("Hitchance (%): {0}%",
                    CastManager.CastCount > 0
                        ? (((float)CastManager.HitCount / CastManager.CastCount) * 100).ToString("00.00")
                        : "n/a"));
        }


        protected override void Volative_OnDrawEnd(EventArgs args)
        {
            DrawDamageIndicator();
            DrawRecalls();
            if (VolatileMenu["debug"].Cast<CheckBox>().CurrentValue)
            {
               DrawDebug();
            }
        }

        private void DrawRecalls()
        {
            if (RecallTracker.Recalls.Any())
            {
                int i = 0;
                RecallTracker.Recall removeme = null;
                foreach (var recall in RecallTracker.Recalls)
                {
                    var Y = RecallTracker.Y() - i;
                    var Y2 = Y + 15;

                    if (!recall.Hero.IsAlly)
                        Drawing.DrawLine(RecallTracker.X(), Y,
                        RecallTracker.X() + (recall.PercentComplete()*_hackMenu["recallwidth"].Cast<Slider>().CurrentValue/100), Y, 16, Color.DarkRed);
                    else
                        Drawing.DrawLine(RecallTracker.X(), Y,
                       RecallTracker.X() + (recall.PercentComplete() * _hackMenu["recallwidth"].Cast<Slider>().CurrentValue / 100), Y, 16, Color.DarkGreen);

                    Vector2[] BoxVectors = new Vector2[5];
                    BoxVectors[0] = new Vector2(RecallTracker.X(), Y2 - 8);
                    BoxVectors[1] = new Vector2(RecallTracker.X(), Y2 + 8);
                    BoxVectors[2] = new Vector2(
                        RecallTracker.X() + _hackMenu["recallwidth"].Cast<Slider>().CurrentValue, Y2 + 8);
                    BoxVectors[3] = new Vector2(
                        RecallTracker.X() + _hackMenu["recallwidth"].Cast<Slider>().CurrentValue, Y2 - 8);

                    BoxVectors[4] = new Vector2(RecallTracker.X(), Y2 - 8);
                    Line.DrawLine(Color.White, BoxVectors);

                    var recallString = "";

                    if (recall.IsAborted)
                        recallString =
                        recall.Hero.ChampionName + " - " + recall.PercentComplete() + "%" + " - Aborted!";
                    else if (recall.PercentComplete() == 100)
                        recallString =
                        recall.Hero.ChampionName + " - " + recall.PercentComplete() + "%" + " - Finished!";
                    else
                        recallString =
                        recall.Hero.ChampionName + " - " + recall.PercentComplete() + "%";

                    Drawing.DrawText(RecallTracker.X() + 10, Y + 8, Color.White, recallString);

                    if (recall.ExpireTime < Environment.TickCount)
                    {
                        removeme = recall;
                    }
                    i += 20;
                }
                if (removeme != null) RecallTracker.Recalls.Remove(removeme);
            }
        }
    }
}