using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections;
using System.Linq;
using System.Text;

namespace CustomVillager
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public GameObject player;
        private string filePath;
        private JObject jsonExistingData;

        private void Awake()
        {
            filePath = Path.Combine(Paths.BepInExRootPath, "plugins/customvillagers.json");
            SceneManager.sceneLoaded += UpdateVillagers;

            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, "{}");
            }

            using (StreamReader r = new StreamReader(filePath))
            {
                jsonExistingData = JObject.Parse(r.ReadToEnd());
            }
        }

        private void Update()
        {
            if (player == null)
            {
                player = GameObject.Find("FPSController/FirstPersonCharacter");
            }
            else if (player != null && !player.GetComponent<InteractionInjectedScript>())
            {
                player.AddComponent<InteractionInjectedScript>();
            }

            if (Input.GetKeyDown(KeyCode.I))
            {
                using (StreamReader r = new StreamReader(filePath))
                {
                    jsonExistingData = JObject.Parse(r.ReadToEnd());
                }

                UpdateVillagers(SceneManager.GetActiveScene(), LoadSceneMode.Single);
            }
        }

        void UpdateVillagers(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (scene.name == "EscenaAssets")
            {
                List<string> villagers = new List<string>(jsonExistingData.Properties().Select(p => p.Name));

                foreach (var name in villagers)
                {
                    var villager = GameObject.Find(name).GetComponentInParent<VillagerCreator>();
                    var tempdata = jsonExistingData[name];

                    villager.DisableAllCustomizationItems();

                    villager.Bottom1 = (bool)tempdata["Bottom1"];
                    villager.Bottom2 = (bool)tempdata["Bottom2"];
                    villager.Top1 = (bool)tempdata["Top1"];
                    villager.Top2 = (bool)tempdata["Top2"];
                    villager.LaurelCrown = (bool)tempdata["LaurelCrown"];
                    villager.ChinBeard = (bool)tempdata["ChinBeard"];
                    villager.Hair = (int)tempdata["Hair"];
                    villager.HairColor = (int)tempdata["HairColor"];
                    villager.Gender = (int)tempdata["Gender"];
                    villager.SkinColor = (int)tempdata["SkinColor"];

                    if ((bool)tempdata["randomize"])
                    {
                        Randomize(villager);
                    }

                    villager.ApplyCustomizationChanges();
                }
            }
        }

        void Randomize(VillagerCreator villager)
        {
            System.Random random = new System.Random();

            villager.ClearCustomizationPreferences();
            villager.Bottom1 = random.NextDouble() >= 0.5;
            villager.Bottom2 = random.NextDouble() >= 0.5;
            villager.Top1 = random.NextDouble() >= 0.5;
            villager.Top2 = random.NextDouble() >= 0.5;
            villager.LaurelCrown = random.NextDouble() >= 0.5;
            villager.ChinBeard = random.NextDouble() >= 0.5;
            villager.Hair = Random.Range(0, villager.Hairs.Length);
            villager.HairColor = Random.Range(0, 5);
            villager.Gender = Random.Range(0, 2);
            villager.SkinColor = Random.Range(0, 4);
        }
    }

    public class InteractionInjectedScript : MonoBehaviour
    {
        string textToShow;
        private GUIStyle guiStyle;

        static string filePath = Path.Combine(Paths.BepInExRootPath, "plugins/customvillagers.json");
        Dictionary<string, JObject> existingData = new Dictionary<string, JObject>();

        void Start()
        {
            guiStyle = new GUIStyle();
            guiStyle.fontSize = 24;
            guiStyle.normal.textColor = Color.white;
            guiStyle.alignment = TextAnchor.MiddleCenter;

            // Lee el archivo JSON y almacena los datos en la memoria.
            if (File.Exists(filePath))
            {
                string jsonData = File.ReadAllText(filePath);
                JObject jsonExistingData = JObject.Parse(jsonData);

                foreach (var pair in jsonExistingData)
                {
                    existingData[pair.Key] = (JObject)pair.Value;
                }
            }
        }

        void Update()
        {
            Ray ray = new Ray(transform.position, transform.forward);
            RaycastHit hitInfo;

            if (Physics.Raycast(ray, out hitInfo, 2f))
            {
                if (hitInfo.transform.CompareTag("Villager") && Input.GetKeyDown(KeyCode.E))
                {
                    string objectPath = GetHierarchyPath(hitInfo.transform);
                    StopAllCoroutines();
                    VillagerCreator villager = hitInfo.transform.GetComponent<VillagerCreator>();

                    if (!existingData.ContainsKey(objectPath))
                    {
                        // Crea una nueva entrada en el diccionario en caso de que no exista.
                        JObject data = new JObject(
                            new JProperty("Bottom1", villager.Bottom1),
                            new JProperty("Bottom2", villager.Bottom2),
                            new JProperty("ChinBeard", villager.ChinBeard),
                            new JProperty("Top1", villager.Top1),
                            new JProperty("Top2", villager.Top2),
                            new JProperty("LaurelCrown", villager.LaurelCrown),
                            new JProperty("Gender", villager.Gender),
                            new JProperty("Hair", villager.Hair),
                            new JProperty("HairColor", villager.HairColor),
                            new JProperty("SkinColor", villager.SkinColor),
                            new JProperty("randomize", false)
                        );

                        existingData[objectPath] = data;

                        // Escribe los datos actualizados en el archivo JSON.
                        File.WriteAllText(filePath, JObject.FromObject(existingData).ToString(Formatting.Indented));
                    }

                    textToShow = objectPath + " (Written to JSON)";

                    StartCoroutine(HideText());
                }
            }
        }

        public static string GetHierarchyPath(Transform transform)
        {
            StringBuilder path = new StringBuilder(transform.name);

            while (transform.parent != null)
            {
                transform = transform.parent;
                path.Insert(0, transform.name + "/");
            }

            return path.ToString();
        }

        void OnGUI()
        {
            Vector2 textSize = guiStyle.CalcSize(new GUIContent(textToShow));
            Rect rect = new Rect((Screen.width - textSize.x) / 2, (Screen.height - textSize.y) / 2, textSize.x, textSize.y);

            GUI.Label(rect, textToShow, guiStyle);
        }

        IEnumerator HideText()
        {
            yield return new WaitForSeconds(2f);
            textToShow = "";
        }
    }
}
