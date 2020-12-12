using Partiality.Modloader;
using UnityEngine;
using System.Reflection;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.ComponentModel.Design;
using System.Reflection.Emit;

namespace AutoDisableDevtools
{
    public class ToggleableDevtools : PartialityMod
    {
        bool active = false;
        bool oDown = false;

        Action<bool> setDevToolsActive;

        public override void OnEnable()
        {
            base.OnEnable();

            var devtools = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name.StartsWith("ExtendedDevtools"));
            if (devtools != null)
            {
                var field = devtools.GetType("ExtendedDevtools.RainWorldHook").GetField("devToolsActive", BindingFlags.Public | BindingFlags.Static);
                var method = new DynamicMethod("_setDevTools", typeof(void), new[] { typeof(bool) }, typeof(ToggleableDevtools).Module);
                var il = method.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Stsfld, field);
                il.Emit(OpCodes.Ret);
                setDevToolsActive = (Action<bool>)method.CreateDelegate(typeof(Action<bool>));
            }
            
            On.RainWorld.Start += RainWorld_Start;
            On.ProcessManager.SwitchMainProcess += ProcessManager_SwitchMainProcess;
        }

        private void UpdateActive(RainWorldGame game)
        {
            if (Input.GetKey(KeyCode.O) && !oDown)
            {
                active = !active;
            }
            oDown = Input.GetKey(KeyCode.O);
            game.devToolsActive = active;
            game.devToolsLabel.isVisible = active;
            setDevToolsActive?.Invoke(active);
        }

        private void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
        {
            orig(self);
            self.buildType = RainWorld.BuildType.Development;
            On.Player.Update += Player_Update;
        }

        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (self.room != null)
            {
                UpdateActive(self.room.game);
            }
        }

        private void ProcessManager_SwitchMainProcess(On.ProcessManager.orig_SwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            orig(self, ID);
            active = false;
            if (self.currentMainLoop is RainWorldGame game)
            {
                UpdateActive(game);
            }
        }
    }
}
