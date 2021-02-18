/*
 * Copyright (c) 2020 The Web eID Project, (c) 2019 The OpenEID Project
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace FirefoxAction
{
    public class FirefoxActions
    {
        [CustomAction]
        public static ActionResult ExtensionSettingsInstall(Session session)
        {
            // Instead of catching and logging errors and returning ActionResult.Failure,
            // we simply let all exceptions through. In both cases the installation fails
            // with error status 1603, but when letting exceptions through the raw exception
            // stack trace will be logged in installer log which gives more information.

            var extensionSettings = GetExtensionSettingsFromSession(session);
            session.Log("Begin ExtensionSettingsInstall " + extensionSettings.UUID);
            using (RegistryKey firefox = Utils.FirefoxKey())
            {
                string[] value = (string[])firefox.GetValue("ExtensionSettings", new string[] { "{}" });
                JObject json = JObject.Parse(string.Join("\n", value));
                json[extensionSettings.UUID] = new JObject
                {
                    ["installation_mode"] = "normal_installed",
                    ["install_url"] = extensionSettings.URL
                };
                firefox.SetValue("ExtensionSettings", json.ToString().Split('\n'));
                return ActionResult.Success;
            }
        }

        [CustomAction]
        public static ActionResult ExtensionSettingsRemove(Session session)
        {
            var extensionSettings = GetExtensionSettingsFromSession(session);
            session.Log("Begin ExtensionSettingsRemove " + extensionSettings.UUID);
            using (RegistryKey firefox = Utils.FirefoxKey())
            {
                string[] value = (string[])firefox.GetValue("ExtensionSettings");
                if (value != null)
                {
                    JObject json = JObject.Parse(string.Join("\n", value));
                    json[extensionSettings.UUID] = new JObject
                    {
                        ["installation_mode"] = "blocked"
                    };
                    firefox.SetValue("ExtensionSettings", json.ToString().Split('\n'));
                }
                return ActionResult.Success;
            }
        }

        private static (string UUID, string URL) GetExtensionSettingsFromSession(Session session)
        {
            // Deferred custom actions cannot directly access installer properties from session,
            // only the CustomActionData property is available, see README how to populate it.
            return (
                session.CustomActionData["EXTENSIONSETTINGS_UUID"],
                session.CustomActionData["EXTENSIONSETTINGS_URL"]
           );
        }
    }

    internal static class Utils
    {
        internal static RegistryKey FirefoxKey()
        {
            using (RegistryKey mozilla = Registry.LocalMachine.OpenOrCreateSubKey(@"Software\Policies\Mozilla", true))
            {
                return mozilla.OpenOrCreateSubKey(@"Firefox", true);
            }
        }

        internal static RegistryKey OpenOrCreateSubKey(this RegistryKey registryKey, string name, bool writable = false)
        {
            return registryKey.OpenSubKey(name, writable) ?? registryKey.CreateSubKey(name);
        }
    }
}
