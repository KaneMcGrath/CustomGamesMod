using ExitGames.Client.Photon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;



namespace CustomGamesMod
{
    public class CGCustomLevelManager
    {
        public CGCustomLevelManager()
        {
            CGCustomLevelManager.count++;
        }

        public void loadAll()
        {
            if (this.done)
            {
                CGCustomLevelManager.log("done.");
                if (CGCustomLevelManager.levelQueue.Contains(this))
                {
                    CGCustomLevelManager.levelQueue.Remove(this);
                }
                return;
            }
            if (!this.sending)
            {
                this.sending = true;
                CGCustomLevelManager.log("sending...");
            }
            if (this.waitTimer > 0f)
            {
                this.waitTimer -= Time.deltaTime;
                return;
            }
            this.waitTimer = CGCustomLevelManager.sendWaitInterval;
            if (CGCustomLevelManager.loadCached && this.reciver == null && !this.ignoreLoadCached)
            {
                PhotonView photonView = FengGameManagerMKII.instance.photonView;
                string[] array = new string[]
                {
                    "loadcached"
                };
                photonView.RPC("customlevelRPC", PhotonTargets.All, new object[]
                {
                    array
                });
                CGCustomLevelManager.log("sent loadcached");
                this.done = true;
                return;
            }
            if (this.reciver == null)
            {
                CGCustomLevelManager.log("Send to all Players");
            }
            else
            {
                CGCustomLevelManager.log("send to Player ID[" + this.reciver.ID.ToString() + "]");
            }
            if (CGCustomLevelManager.currentLevel.Count > this.index * CGCustomLevelManager.sendsPerInterval + CGCustomLevelManager.sendsPerInterval)
            {
                string[] array2 = new string[CGCustomLevelManager.sendsPerInterval];
                for (int i = 0; i < CGCustomLevelManager.sendsPerInterval; i++)
                {
                    array2[i] = CGCustomLevelManager.currentLevel[this.index * CGCustomLevelManager.sendsPerInterval + i];
                    CGCustomLevelManager.log("added:" + array2[i].Substring(0, 15));
                }
                this.sendRPC(array2);
                CGCustomLevelManager.log("Index: " + this.index.ToString() + " finished");
            }
            else if (CGCustomLevelManager.currentLevel.Count > this.index * CGCustomLevelManager.sendsPerInterval)
            {
                int num = CGCustomLevelManager.currentLevel.Count - this.index * CGCustomLevelManager.sendsPerInterval;
                string[] array3 = new string[num];
                for (int j = 0; j < num; j++)
                {
                    array3[j] = CGCustomLevelManager.currentLevel[this.index * CGCustomLevelManager.sendsPerInterval + j];
                }
                this.sendRPC(array3);
            }
            else
            {
                this.done = true;
                if (CGCustomLevelManager.levelQueue.Contains(this))
                {
                    CGCustomLevelManager.levelQueue.Remove(this);
                    CGCustomLevelManager.log("removed");
                }
            }
            this.index++;
        }

        public void cancel()
        {
            this.done = true;
        }

        public CGCustomLevelManager(PhotonPlayer reciver)
        {
            this.reciver = reciver;
            CGCustomLevelManager.count++;
        }

        public void sendRPC(string[] array)
        {
            PhotonPlayer[] playerList = PhotonNetwork.playerList;
            if (this.reciver == null)
            {
                for (int i = 0; i < playerList.Length; i++)
                {
                    FengGameManagerMKII.instance.photonView.RPC("customlevelRPC", playerList[i], new object[]
                    {
                        array
                    });
                }
                return;
            }
            bool flag = false;
            for (int j = 0; j < playerList.Length; j++)
            {
                if (playerList[j].Equals(this.reciver))
                {
                    flag = true;
                }
            }
            if (flag)
            {
                FengGameManagerMKII.instance.photonView.RPC("customlevelRPC", this.reciver, new object[]
                {
                    array
                });
                return;
            }
            this.cancel();
        }

        static CGCustomLevelManager()
        {
            CGCustomLevelManager.levelQueue = new List<CGCustomLevelManager>();
            CGCustomLevelManager.sendWaitInterval = 2f;
            CGCustomLevelManager.sendsPerInterval = 5;
            CGCustomLevelManager.currentLevel = new List<string>();
        }

        public static void OnPhotonPlayerConnected(PhotonPlayer player)
        {
            if (PhotonNetwork.isMasterClient && CGCustomLevelManager.useCustomLevel)
            {
                CGCustomLevelManager.log("PhotonPlayer:" + player.ID.ToString());
                CGCustomLevelManager item = new CGCustomLevelManager(player);
                CGCustomLevelManager.levelQueue.Add(item);
            }
        }

        public static void OnRestart()
        {
            if (CGCustomLevelManager.useCustomLevel)
            {
                CGCustomLevelManager item = new CGCustomLevelManager();
                CGCustomLevelManager.levelQueue.Add(item);
            }
        }

        public static void Update()
        {
            if (CGCustomLevelManager.useCustomLevel && CGCustomLevelManager.levelQueue.Count != 0 && FengGameManagerMKII.instance.gameStart && PhotonNetwork.isMasterClient)
            {
                if (CGCustomLevelManager.levelQueue[0].done)
                {
                    CGCustomLevelManager.levelQueue.RemoveAt(0);
                }
                CGCustomLevelManager.levelQueue[0].loadAll();
            }
        }

        public static void load()
        {
            foreach (string text in File.ReadAllText("level.txt").Replace(Environment.NewLine, "").Split(new char[]
            {
                ';'
            }))
            {
                if (text != "")
                {
                    CGCustomLevelManager.log(text);
                    CGCustomLevelManager.currentLevel.Add(text);
                }
            }
        }

        public static void log(string s)
        {
            CGLog.log(s);
        }

        public static void ChangeMap(string[] map)
        {
            string[] array = new string[]
            {
                "loadempty"
            };
            FengGameManagerMKII.instance.photonView.RPC("customlevelRPC", PhotonTargets.All, new object[]
            {
                array
            });
            CGCustomLevelManager.currentLevel.Clear();
            CGCustomLevelManager.currentLevel.AddRange(map);
            CGCustomLevelManager.useCustomLevel = false;
            FengGameManagerMKII.instance.restartGame2(false);
            CGCustomLevelManager.useCustomLevel = true;
            CGCustomLevelManager item = new CGCustomLevelManager
            {
                ignoreLoadCached = true
            };
            CGCustomLevelManager.levelQueue.Add(item);
        }

        public static void previewLevel(string[] content)
        {
            if (CGCustomLevelManager.previewObjects.Count > 0)
            {
                foreach (GameObject obj in CGCustomLevelManager.previewObjects)
                {
                    UnityEngine.Object.Destroy(obj);
                }
            }
            for (int i = 0; i < content.Length; i++)
            {
                string[] array = content[i].Split(new char[]
                {
                    ','
                });
                if (array[0].StartsWith("custom"))
                {
                    float a = 1f;
                    GameObject gameObject = (GameObject)UnityEngine.Object.Instantiate((GameObject)FengGameManagerMKII.RCassets.Load(array[1]), new Vector3(Convert.ToSingle(array[12]), Convert.ToSingle(array[13]), Convert.ToSingle(array[14])), new Quaternion(Convert.ToSingle(array[15]), Convert.ToSingle(array[16]), Convert.ToSingle(array[17]), Convert.ToSingle(array[18])));
                    CGCustomLevelManager.previewObjects.Add(gameObject);
                    if (array[2] != "default")
                    {
                        if (array[2].StartsWith("transparent"))
                        {
                            float num;
                            if (float.TryParse(array[2].Substring(11), out num))
                            {
                                a = num;
                            }
                            foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>())
                            {
                                renderer.material = (Material)FengGameManagerMKII.RCassets.Load("transparent");
                                if (Convert.ToSingle(array[10]) != 1f || Convert.ToSingle(array[11]) != 1f)
                                {
                                    renderer.material.mainTextureScale = new Vector2(renderer.material.mainTextureScale.x * Convert.ToSingle(array[10]), renderer.material.mainTextureScale.y * Convert.ToSingle(array[11]));
                                }
                            }
                        }
                        else
                        {
                            foreach (Renderer renderer2 in gameObject.GetComponentsInChildren<Renderer>())
                            {
                                renderer2.material = (Material)FengGameManagerMKII.RCassets.Load(array[2]);
                                if (Convert.ToSingle(array[10]) != 1f || Convert.ToSingle(array[11]) != 1f)
                                {
                                    renderer2.material.mainTextureScale = new Vector2(renderer2.material.mainTextureScale.x * Convert.ToSingle(array[10]), renderer2.material.mainTextureScale.y * Convert.ToSingle(array[11]));
                                }
                            }
                        }
                    }
                    float num2 = gameObject.transform.localScale.x * Convert.ToSingle(array[3]);
                    num2 -= 0.001f;
                    float y = gameObject.transform.localScale.y * Convert.ToSingle(array[4]);
                    float z = gameObject.transform.localScale.z * Convert.ToSingle(array[5]);
                    gameObject.transform.localScale = new Vector3(num2, y, z);
                    if (array[6] != "0")
                    {
                        Color color = new Color(Convert.ToSingle(array[7]), Convert.ToSingle(array[8]), Convert.ToSingle(array[9]), a);
                        MeshFilter[] componentsInChildren2 = gameObject.GetComponentsInChildren<MeshFilter>();
                        for (int k = 0; k < componentsInChildren2.Length; k++)
                        {
                            Mesh mesh = componentsInChildren2[k].mesh;
                            Color[] array2 = new Color[mesh.vertexCount];
                            for (int l = 0; l < mesh.vertexCount; l++)
                            {
                                array2[l] = color;
                            }
                            mesh.colors = array2;
                        }
                    }
                }
                else if (array[0].StartsWith("base"))
                {
                    if (array.Length < 15)
                    {
                        GameObject item = (GameObject)UnityEngine.Object.Instantiate(Resources.Load(array[1]), new Vector3(Convert.ToSingle(array[2]), Convert.ToSingle(array[3]), Convert.ToSingle(array[4])), new Quaternion(Convert.ToSingle(array[5]), Convert.ToSingle(array[6]), Convert.ToSingle(array[7]), Convert.ToSingle(array[8])));
                        CGCustomLevelManager.previewObjects.Add(item);
                    }
                    else
                    {
                        float a2 = 1f;
                        GameObject gameObject2 = (GameObject)UnityEngine.Object.Instantiate((GameObject)Resources.Load(array[1]), new Vector3(Convert.ToSingle(array[12]), Convert.ToSingle(array[13]), Convert.ToSingle(array[14])), new Quaternion(Convert.ToSingle(array[15]), Convert.ToSingle(array[16]), Convert.ToSingle(array[17]), Convert.ToSingle(array[18])));
                        CGCustomLevelManager.previewObjects.Add(gameObject2);
                        if (array[2] != "default")
                        {
                            if (array[2].StartsWith("transparent"))
                            {
                                float num3;
                                if (float.TryParse(array[2].Substring(11), out num3))
                                {
                                    a2 = num3;
                                }
                                foreach (Renderer renderer3 in gameObject2.GetComponentsInChildren<Renderer>())
                                {
                                    renderer3.material = (Material)FengGameManagerMKII.RCassets.Load("transparent");
                                    if (Convert.ToSingle(array[10]) != 1f || Convert.ToSingle(array[11]) != 1f)
                                    {
                                        renderer3.material.mainTextureScale = new Vector2(renderer3.material.mainTextureScale.x * Convert.ToSingle(array[10]), renderer3.material.mainTextureScale.y * Convert.ToSingle(array[11]));
                                    }
                                }
                            }
                            else
                            {
                                foreach (Renderer renderer4 in gameObject2.GetComponentsInChildren<Renderer>())
                                {
                                    if (!renderer4.name.Contains("Particle System") || !gameObject2.name.Contains("aot_supply"))
                                    {
                                        renderer4.material = (Material)FengGameManagerMKII.RCassets.Load(array[2]);
                                        if (Convert.ToSingle(array[10]) != 1f || Convert.ToSingle(array[11]) != 1f)
                                        {
                                            renderer4.material.mainTextureScale = new Vector2(renderer4.material.mainTextureScale.x * Convert.ToSingle(array[10]), renderer4.material.mainTextureScale.y * Convert.ToSingle(array[11]));
                                        }
                                    }
                                }
                            }
                        }
                        float num4 = gameObject2.transform.localScale.x * Convert.ToSingle(array[3]);
                        num4 -= 0.001f;
                        float y2 = gameObject2.transform.localScale.y * Convert.ToSingle(array[4]);
                        float z2 = gameObject2.transform.localScale.z * Convert.ToSingle(array[5]);
                        gameObject2.transform.localScale = new Vector3(num4, y2, z2);
                        if (array[6] != "0")
                        {
                            Color color2 = new Color(Convert.ToSingle(array[7]), Convert.ToSingle(array[8]), Convert.ToSingle(array[9]), a2);
                            MeshFilter[] componentsInChildren3 = gameObject2.GetComponentsInChildren<MeshFilter>();
                            for (int m = 0; m < componentsInChildren3.Length; m++)
                            {
                                Mesh mesh2 = componentsInChildren3[m].mesh;
                                Color[] array3 = new Color[mesh2.vertexCount];
                                for (int n = 0; n < mesh2.vertexCount; n++)
                                {
                                    array3[n] = color2;
                                }
                                mesh2.colors = array3;
                            }
                        }
                    }
                }
                else if (array[0].StartsWith("misc"))
                {
                    if (array[1].StartsWith("barrier"))
                    {
                        GameObject gameObject3 = (GameObject)UnityEngine.Object.Instantiate((GameObject)FengGameManagerMKII.RCassets.Load(array[1]), new Vector3(Convert.ToSingle(array[5]), Convert.ToSingle(array[6]), Convert.ToSingle(array[7])), new Quaternion(Convert.ToSingle(array[8]), Convert.ToSingle(array[9]), Convert.ToSingle(array[10]), Convert.ToSingle(array[11])));
                        CGCustomLevelManager.previewObjects.Add(gameObject3);
                        float num5 = gameObject3.transform.localScale.x * Convert.ToSingle(array[2]);
                        num5 -= 0.001f;
                        float y3 = gameObject3.transform.localScale.y * Convert.ToSingle(array[3]);
                        float z3 = gameObject3.transform.localScale.z * Convert.ToSingle(array[4]);
                        gameObject3.transform.localScale = new Vector3(num5, y3, z3);
                    }
                }
                else if (array[0].StartsWith("racing"))
                {
                    if (array[1].StartsWith("start"))
                    {
                        GameObject gameObject4 = (GameObject)UnityEngine.Object.Instantiate((GameObject)FengGameManagerMKII.RCassets.Load(array[1]), new Vector3(Convert.ToSingle(array[5]), Convert.ToSingle(array[6]), Convert.ToSingle(array[7])), new Quaternion(Convert.ToSingle(array[8]), Convert.ToSingle(array[9]), Convert.ToSingle(array[10]), Convert.ToSingle(array[11])));
                        CGCustomLevelManager.previewObjects.Add(gameObject4);
                        float num6 = gameObject4.transform.localScale.x * Convert.ToSingle(array[2]);
                        num6 -= 0.001f;
                        float y4 = gameObject4.transform.localScale.y * Convert.ToSingle(array[3]);
                        float z4 = gameObject4.transform.localScale.z * Convert.ToSingle(array[4]);
                        gameObject4.transform.localScale = new Vector3(num6, y4, z4);
                    }
                    else if (array[1].StartsWith("end"))
                    {
                        GameObject gameObject5 = (GameObject)UnityEngine.Object.Instantiate((GameObject)FengGameManagerMKII.RCassets.Load(array[1]), new Vector3(Convert.ToSingle(array[5]), Convert.ToSingle(array[6]), Convert.ToSingle(array[7])), new Quaternion(Convert.ToSingle(array[8]), Convert.ToSingle(array[9]), Convert.ToSingle(array[10]), Convert.ToSingle(array[11])));
                        CGCustomLevelManager.previewObjects.Add(gameObject5);
                        float num7 = gameObject5.transform.localScale.x * Convert.ToSingle(array[2]);
                        num7 -= 0.001f;
                        float y5 = gameObject5.transform.localScale.y * Convert.ToSingle(array[3]);
                        float z5 = gameObject5.transform.localScale.z * Convert.ToSingle(array[4]);
                        gameObject5.transform.localScale = new Vector3(num7, y5, z5);
                        gameObject5.GetComponentInChildren<Collider>().gameObject.AddComponent<LevelTriggerRacingEnd>();
                    }
                    else if (array[1].StartsWith("kill"))
                    {
                        GameObject gameObject6 = (GameObject)UnityEngine.Object.Instantiate((GameObject)FengGameManagerMKII.RCassets.Load(array[1]), new Vector3(Convert.ToSingle(array[5]), Convert.ToSingle(array[6]), Convert.ToSingle(array[7])), new Quaternion(Convert.ToSingle(array[8]), Convert.ToSingle(array[9]), Convert.ToSingle(array[10]), Convert.ToSingle(array[11])));
                        CGCustomLevelManager.previewObjects.Add(gameObject6);
                        float num8 = gameObject6.transform.localScale.x * Convert.ToSingle(array[2]);
                        num8 -= 0.001f;
                        float y6 = gameObject6.transform.localScale.y * Convert.ToSingle(array[3]);
                        float z6 = gameObject6.transform.localScale.z * Convert.ToSingle(array[4]);
                        gameObject6.transform.localScale = new Vector3(num8, y6, z6);
                        gameObject6.GetComponentInChildren<Collider>().gameObject.AddComponent<RacingKillTrigger>();
                    }
                    else if (array[1].StartsWith("checkpoint"))
                    {
                        GameObject gameObject7 = (GameObject)UnityEngine.Object.Instantiate((GameObject)FengGameManagerMKII.RCassets.Load(array[1]), new Vector3(Convert.ToSingle(array[5]), Convert.ToSingle(array[6]), Convert.ToSingle(array[7])), new Quaternion(Convert.ToSingle(array[8]), Convert.ToSingle(array[9]), Convert.ToSingle(array[10]), Convert.ToSingle(array[11])));
                        CGCustomLevelManager.previewObjects.Add(gameObject7);
                        float num9 = gameObject7.transform.localScale.x * Convert.ToSingle(array[2]);
                        num9 -= 0.001f;
                        float y7 = gameObject7.transform.localScale.y * Convert.ToSingle(array[3]);
                        float z7 = gameObject7.transform.localScale.z * Convert.ToSingle(array[4]);
                        gameObject7.transform.localScale = new Vector3(num9, y7, z7);
                        gameObject7.GetComponentInChildren<Collider>().gameObject.AddComponent<RacingCheckpointTrigger>();
                    }
                }
                else if (array[0].StartsWith("map"))
                {
                    if (array[1].StartsWith("disablebounds"))
                    {
                        UnityEngine.Object.Destroy(GameObject.Find("gameobjectOutSide"));
                        UnityEngine.Object.Instantiate(FengGameManagerMKII.RCassets.Load("outside"));
                    }
                }
                else if (PhotonNetwork.isMasterClient && array[0].StartsWith("photon") && array[1].StartsWith("Cannon"))
                {
                    if (array.Length > 15)
                    {
                        GameObject gameObject8 = PhotonNetwork.Instantiate("RCAsset/" + array[1] + "Prop", new Vector3(Convert.ToSingle(array[12]), Convert.ToSingle(array[13]), Convert.ToSingle(array[14])), new Quaternion(Convert.ToSingle(array[15]), Convert.ToSingle(array[16]), Convert.ToSingle(array[17]), Convert.ToSingle(array[18])), 0);
                        CGCustomLevelManager.previewObjects.Add(gameObject8);
                        gameObject8.GetComponent<CannonPropRegion>().settings = content[i];
                        gameObject8.GetPhotonView().RPC("SetSize", PhotonTargets.AllBuffered, new object[]
                        {
                            content[i]
                        });
                    }
                    else
                    {
                        GameObject gameObject9 = PhotonNetwork.Instantiate("RCAsset/" + array[1] + "Prop", new Vector3(Convert.ToSingle(array[2]), Convert.ToSingle(array[3]), Convert.ToSingle(array[4])), new Quaternion(Convert.ToSingle(array[5]), Convert.ToSingle(array[6]), Convert.ToSingle(array[7]), Convert.ToSingle(array[8])), 0);
                        gameObject9.GetComponent<CannonPropRegion>().settings = content[i];
                        CGCustomLevelManager.previewObjects.Add(gameObject9);
                    }
                }
            }
        }

        public PhotonPlayer reciver;

        private int index;

        private bool sending;

        public bool done;

        private float waitTimer = 2f;

        public static List<CGCustomLevelManager> levelQueue;

        public static int count;

        public static float sendWaitInterval;

        public static int sendsPerInterval;

        public static List<string> currentLevel;

        public static bool loadCached = true;

        public static bool useCustomLevel = false;

        private bool ignoreLoadCached;

        private static List<GameObject> previewObjects = new List<GameObject>();
    }

    public class CGLevelEditor
    {
        public static void CustomControlsGUI()
        {
            FlatUI.Box(new Rect(5f, 590f, 295f, 300f));
        }
        public static void OnGUI()
        {
            float num = (float)Screen.width - 600f;
            FlatUI.Box(new Rect(300f, 5f, 200f, 30f));
            
            if (!Screen.lockCursor)
            {
                GUI.Label(new Rect(300f, 5f, 200f, 30f), "Camera Lock [" + (string)FengGameManagerMKII.settings[123] + "]", CGSettingsMenu.selectedLevelPathStyle);
                FlatUI.Box(new Rect(500f, 5f, 100f, 30f), FlatUI.Oarnge);
                GUI.Label(new Rect(500f, 5f, 100f, 30f), "Locked", CGSettingsMenu.levelPathStyle);
            }
            else
            {
                if (FengGameManagerMKII.instance.selectedObj == null)
                {
                    GUI.Label(new Rect(300f, 5f, 200f, 30f), "Camera Lock [" + (string)FengGameManagerMKII.settings[123] + "]", CGSettingsMenu.selectedLevelPathStyle);
                    FlatUI.Box(new Rect(500f, 5f, 100f, 30f));
                    GUI.Label(new Rect(500f, 5f, 100f, 30f), "Free", CGSettingsMenu.levelPathStyle);
                }
                else
                {
                    GUI.Label(new Rect(300f, 5f, 200f, 30f), "Camera Lock [" + FengGameManagerMKII.inputRC.levelKeys[InputCodeRC.levelPlace].ToString() + "]", CGSettingsMenu.selectedLevelPathStyle);
                    FlatUI.Box(new Rect(500f, 5f, 100f, 30f), CGSettingsMenu.magenta);
                    GUI.Label(new Rect(500f, 5f, 100f, 30f), "Selected", CGSettingsMenu.levelPathStyle);
                }
            }
            FlatUI.Box(new Rect(num, 5f, 300f, 30f));
            if (GUI.Button(new Rect(num, 5f, 300f, 30f), "Load Map"))
            {
                toggleLevelsDropDown = !toggleLevelsDropDown;
            }
            FlatUI.Box(new Rect(300f, 35f, 300f, 30f));
            if (GUI.Button(new Rect(300f, 35f, 150f, 30f), "Load Level"))
            {
                toggleLevelLoadDialoge = !toggleLevelLoadDialoge;
                toggleLevelSaveDialoge = false;
            }
            if (GUI.Button(new Rect(450f, 35f, 150f, 30f), "Save Level"))
            {
                toggleLevelSaveDialoge = !toggleLevelSaveDialoge;
                toggleLevelLoadDialoge = false;
            }
            if (toggleLevelsDropDown)
            {
                FlatUI.Box(new Rect(num, 35f, 300f, 820f));
                for (int i = 0; i < LevelInfo.levels.Length; i++)
                {
                    if (GUI.Button(new Rect(num + 5f, 35f + (float)i * 30f, 290f, 30f), LevelInfo.levels[i].name))
                    {
                        FengGameManagerMKII.level = LevelInfo.levels[i].mapName;
                        if (!LevelInfo.levels[i].name.StartsWith("Custom"))
                        {
                            skipTreeDeleting = true;
                        }
                        else
                        {
                            skipTreeDeleting = false;
                        }
                        Application.LoadLevel(LevelInfo.levels[i].mapName);
                        toggleLevelsDropDown = false;
                    }
                }
            }
            if (toggleLevelLoadDialoge)
            {
                CGSettingsMenu.updateLevels();
                FlatUI.Box(new Rect(300f, 65f, 300f, 600f));
                int rowCount = 19;
                Rect[] array = new Rect[rowCount];
                for (int j = 0; j < rowCount; j++)
                {
                    array[j] = new Rect(300f, 65f + 30f * (float)j, 300f, 30f);
                }
                if (GUI.Button(new Rect(400f, 635f, 200f, 30f), "Return"))
                {
                    toggleLevelLoadDialoge = false;
                }
                if (CGSettingsMenu.levels.Length != 0)
                {
                    int start = LoadLevelDialogePage * rowCount;
                    int end = Math.Min(start + rowCount - 1, CGSettingsMenu.levels.Length - 1);
                    Math.Min(start + rowCount - 1, CGSettingsMenu.levels.Length - 1);
                    for (int k = start; k <= end; k++)
                    {
                        if (GUI.Button(array[k - rowCount * LoadLevelDialogePage], CGSettingsMenu.levels[k]))
                        {
                            loadLevel(File.ReadAllText(CGSettingsMenu.levels[k]));
                            toggleLevelLoadDialoge = false;
                        }
                    }
                    if (CGSettingsMenu.levels.Length > rowCount)
                    {
                        if (LoadLevelDialogePage > 0 && GUI.Button(new Rect(300f, 635f, 50f, 30f), "<"))
                        {
                            LoadLevelDialogePage--;
                        }
                        float num5 = Mathf.Ceil((float)CGSettingsMenu.levels.Length / (float)rowCount);
                        if ((float)LoadLevelDialogePage < num5 - 1f && GUI.Button(new Rect(350f, 635f, 50f, 30f), ">"))
                        {
                            LoadLevelDialogePage++;
                            return;
                        }
                    }
                }
            }
            if (toggleLevelSaveDialoge)
            {
                FlatUI.Box(new Rect(300f, 65f, 300f, 150f));
                GUI.Label(new Rect(300f, 65f, 300f, 30f), "Level Name", CGSettingsMenu.levelPathStyle);
                LevelSaveName = GUI.TextArea(new Rect(300f, 95f, 300f, 30f), LevelSaveName);
                if (File.Exists("levels/" + LevelSaveName + ".txt"))
                {
                    GUI.Label(new Rect(300f, 125f, 300f, 30f), "Level already exists!", CGSettingsMenu.selectedLevelPathStyle);
                    GUI.Label(new Rect(300f, 155f, 300f, 30f), "Do you want to overwrite?", CGSettingsMenu.selectedLevelPathStyle);
                }
                if (GUI.Button(new Rect(300f, 185f, 150f, 30f), "Cancel"))
                {
                    LevelSaveName = "";
                    toggleLevelSaveDialoge = false;
                }
                if (GUI.Button(new Rect(450f, 185f, 150f, 30f), "Save"))
                {
                    string text = string.Empty;
                    int num6 = 0;
                    foreach (object obj in FengGameManagerMKII.linkHash[3].Values)
                    {
                        string str = (string)obj;
                        num6++;
                        text = text + str + ";\n";
                    }
                    File.WriteAllText("levels/" + LevelSaveName + ".txt", text);
                    LevelSaveName = "";
                    toggleLevelSaveDialoge = false;
                }
            }
        }

        public static void Start()
        {
        }

        public static void loadLevel(string text)
        {
            FengGameManagerMKII.settings[77] = text;
            foreach (GameObject gameObject in UnityEngine.Object.FindObjectsOfType(typeof(GameObject)))
            {
                if (gameObject.name.StartsWith("custom") || gameObject.name.StartsWith("base") || gameObject.name.StartsWith("photon") || gameObject.name.StartsWith("spawnpoint") || gameObject.name.StartsWith("misc") || gameObject.name.StartsWith("racing"))
                {
                    UnityEngine.Object.Destroy(gameObject);
                }
            }
            FengGameManagerMKII.linkHash[3].Clear();
            FengGameManagerMKII.settings[186] = 0;
            string[] array2 = Regex.Replace((string)FengGameManagerMKII.settings[77], "\\s+", "").Replace("\r\n", "").Replace("\n", "").Replace("\r", "").Split(new char[]
            {
                ';'
            });
            for (int j = 0; j < array2.Length; j++)
            {
                string[] array3 = array2[j].Split(new char[]
                {
                    ','
                });
                if (array3[0].StartsWith("custom") || array3[0].StartsWith("base") || array3[0].StartsWith("photon") || array3[0].StartsWith("spawnpoint") || array3[0].StartsWith("misc") || array3[0].StartsWith("racing"))
                {
                    GameObject gameObject2 = null;
                    if (array3[0].StartsWith("custom"))
                    {
                        gameObject2 = (GameObject)UnityEngine.Object.Instantiate((GameObject)FengGameManagerMKII.RCassets.Load(array3[1]), new Vector3(Convert.ToSingle(array3[12]), Convert.ToSingle(array3[13]), Convert.ToSingle(array3[14])), new Quaternion(Convert.ToSingle(array3[15]), Convert.ToSingle(array3[16]), Convert.ToSingle(array3[17]), Convert.ToSingle(array3[18])));
                    }
                    else if (array3[0].StartsWith("photon"))
                    {
                        if (array3[1].StartsWith("Cannon"))
                        {
                            if (array3.Length < 15)
                            {
                                gameObject2 = (GameObject)UnityEngine.Object.Instantiate((GameObject)FengGameManagerMKII.RCassets.Load(array3[1] + "Prop"), new Vector3(Convert.ToSingle(array3[2]), Convert.ToSingle(array3[3]), Convert.ToSingle(array3[4])), new Quaternion(Convert.ToSingle(array3[5]), Convert.ToSingle(array3[6]), Convert.ToSingle(array3[7]), Convert.ToSingle(array3[8])));
                            }
                            else
                            {
                                gameObject2 = (GameObject)UnityEngine.Object.Instantiate((GameObject)FengGameManagerMKII.RCassets.Load(array3[1] + "Prop"), new Vector3(Convert.ToSingle(array3[12]), Convert.ToSingle(array3[13]), Convert.ToSingle(array3[14])), new Quaternion(Convert.ToSingle(array3[15]), Convert.ToSingle(array3[16]), Convert.ToSingle(array3[17]), Convert.ToSingle(array3[18])));
                            }
                        }
                        else
                        {
                            gameObject2 = (GameObject)UnityEngine.Object.Instantiate((GameObject)FengGameManagerMKII.RCassets.Load(array3[1]), new Vector3(Convert.ToSingle(array3[4]), Convert.ToSingle(array3[5]), Convert.ToSingle(array3[6])), new Quaternion(Convert.ToSingle(array3[7]), Convert.ToSingle(array3[8]), Convert.ToSingle(array3[9]), Convert.ToSingle(array3[10])));
                        }
                    }
                    else if (array3[0].StartsWith("spawnpoint"))
                    {
                        gameObject2 = (GameObject)UnityEngine.Object.Instantiate((GameObject)FengGameManagerMKII.RCassets.Load(array3[1]), new Vector3(Convert.ToSingle(array3[2]), Convert.ToSingle(array3[3]), Convert.ToSingle(array3[4])), new Quaternion(Convert.ToSingle(array3[5]), Convert.ToSingle(array3[6]), Convert.ToSingle(array3[7]), Convert.ToSingle(array3[8])));
                    }
                    else if (array3[0].StartsWith("base"))
                    {
                        if (array3.Length < 15)
                        {
                            gameObject2 = (GameObject)UnityEngine.Object.Instantiate((GameObject)Resources.Load(array3[1]), new Vector3(Convert.ToSingle(array3[2]), Convert.ToSingle(array3[3]), Convert.ToSingle(array3[4])), new Quaternion(Convert.ToSingle(array3[5]), Convert.ToSingle(array3[6]), Convert.ToSingle(array3[7]), Convert.ToSingle(array3[8])));
                        }
                        else
                        {
                            gameObject2 = (GameObject)UnityEngine.Object.Instantiate((GameObject)Resources.Load(array3[1]), new Vector3(Convert.ToSingle(array3[12]), Convert.ToSingle(array3[13]), Convert.ToSingle(array3[14])), new Quaternion(Convert.ToSingle(array3[15]), Convert.ToSingle(array3[16]), Convert.ToSingle(array3[17]), Convert.ToSingle(array3[18])));
                        }
                    }
                    else if (array3[0].StartsWith("misc"))
                    {
                        if (array3[1].StartsWith("barrier"))
                        {
                            gameObject2 = (GameObject)UnityEngine.Object.Instantiate((GameObject)FengGameManagerMKII.RCassets.Load("barrierEditor"), new Vector3(Convert.ToSingle(array3[5]), Convert.ToSingle(array3[6]), Convert.ToSingle(array3[7])), new Quaternion(Convert.ToSingle(array3[8]), Convert.ToSingle(array3[9]), Convert.ToSingle(array3[10]), Convert.ToSingle(array3[11])));
                        }
                        else if (array3[1].StartsWith("region"))
                        {
                            gameObject2 = (GameObject)UnityEngine.Object.Instantiate((GameObject)FengGameManagerMKII.RCassets.Load("regionEditor"));
                            gameObject2.transform.position = new Vector3(Convert.ToSingle(array3[6]), Convert.ToSingle(array3[7]), Convert.ToSingle(array3[8]));
                            GameObject gameObject3 = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("UI/LabelNameOverHead"));
                            gameObject3.name = "RegionLabel";
                            gameObject3.transform.parent = gameObject2.transform;
                            float y = 1f;
                            if (Convert.ToSingle(array3[4]) > 100f)
                            {
                                y = 0.8f;
                            }
                            else if (Convert.ToSingle(array3[4]) > 1000f)
                            {
                                y = 0.5f;
                            }
                            gameObject3.transform.localPosition = new Vector3(0f, y, 0f);
                            gameObject3.transform.localScale = new Vector3(5f / Convert.ToSingle(array3[3]), 5f / Convert.ToSingle(array3[4]), 5f / Convert.ToSingle(array3[5]));
                            gameObject3.GetComponent<UILabel>().text = array3[2];
                            gameObject2.AddComponent<RCRegionLabel>();
                            gameObject2.GetComponent<RCRegionLabel>().myLabel = gameObject3;
                        }
                        else if (array3[1].StartsWith("racingStart"))
                        {
                            gameObject2 = (GameObject)UnityEngine.Object.Instantiate((GameObject)FengGameManagerMKII.RCassets.Load("racingStart"), new Vector3(Convert.ToSingle(array3[5]), Convert.ToSingle(array3[6]), Convert.ToSingle(array3[7])), new Quaternion(Convert.ToSingle(array3[8]), Convert.ToSingle(array3[9]), Convert.ToSingle(array3[10]), Convert.ToSingle(array3[11])));
                        }
                        else if (array3[1].StartsWith("racingEnd"))
                        {
                            gameObject2 = (GameObject)UnityEngine.Object.Instantiate((GameObject)FengGameManagerMKII.RCassets.Load("racingEnd"), new Vector3(Convert.ToSingle(array3[5]), Convert.ToSingle(array3[6]), Convert.ToSingle(array3[7])), new Quaternion(Convert.ToSingle(array3[8]), Convert.ToSingle(array3[9]), Convert.ToSingle(array3[10]), Convert.ToSingle(array3[11])));
                        }
                    }
                    else if (array3[0].StartsWith("racing"))
                    {
                        gameObject2 = (GameObject)UnityEngine.Object.Instantiate((GameObject)FengGameManagerMKII.RCassets.Load(array3[1]), new Vector3(Convert.ToSingle(array3[5]), Convert.ToSingle(array3[6]), Convert.ToSingle(array3[7])), new Quaternion(Convert.ToSingle(array3[8]), Convert.ToSingle(array3[9]), Convert.ToSingle(array3[10]), Convert.ToSingle(array3[11])));
                    }
                    if (array3[2] != "default" && (array3[0].StartsWith("custom") || (array3[0].StartsWith("base") && array3.Length > 15) || (array3[0].StartsWith("photon") && array3.Length > 15)))
                    {
                        foreach (Renderer renderer in gameObject2.GetComponentsInChildren<Renderer>())
                        {
                            if (!renderer.name.Contains("Particle System") || !gameObject2.name.Contains("aot_supply"))
                            {
                                renderer.material = (Material)FengGameManagerMKII.RCassets.Load(array3[2]);
                                renderer.material.mainTextureScale = new Vector2(renderer.material.mainTextureScale.x * Convert.ToSingle(array3[10]), renderer.material.mainTextureScale.y * Convert.ToSingle(array3[11]));
                            }
                        }
                    }
                    if (array3[0].StartsWith("custom") || (array3[0].StartsWith("base") && array3.Length > 15) || (array3[0].StartsWith("photon") && array3.Length > 15))
                    {
                        float num = gameObject2.transform.localScale.x * Convert.ToSingle(array3[3]);
                        num -= 0.001f;
                        float y2 = gameObject2.transform.localScale.y * Convert.ToSingle(array3[4]);
                        float z = gameObject2.transform.localScale.z * Convert.ToSingle(array3[5]);
                        gameObject2.transform.localScale = new Vector3(num, y2, z);
                        if (array3[6] != "0")
                        {
                            Color color = new Color(Convert.ToSingle(array3[7]), Convert.ToSingle(array3[8]), Convert.ToSingle(array3[9]), 1f);
                            MeshFilter[] componentsInChildren2 = gameObject2.GetComponentsInChildren<MeshFilter>();
                            for (int k = 0; k < componentsInChildren2.Length; k++)
                            {
                                Mesh mesh = componentsInChildren2[k].mesh;
                                Color[] array4 = new Color[mesh.vertexCount];
                                for (int l = 0; l < mesh.vertexCount; l++)
                                {
                                    array4[l] = color;
                                }
                                mesh.colors = array4;
                            }
                        }
                        gameObject2.name = string.Concat(new string[]
                        {
                            array3[0],
                            ",",
                            array3[1],
                            ",",
                            array3[2],
                            ",",
                            array3[3],
                            ",",
                            array3[4],
                            ",",
                            array3[5],
                            ",",
                            array3[6],
                            ",",
                            array3[7],
                            ",",
                            array3[8],
                            ",",
                            array3[9],
                            ",",
                            array3[10],
                            ",",
                            array3[11]
                        });
                    }
                    else if (array3[0].StartsWith("misc"))
                    {
                        if (array3[1].StartsWith("barrier") || array3[1].StartsWith("racing"))
                        {
                            float num2 = gameObject2.transform.localScale.x * Convert.ToSingle(array3[2]);
                            num2 -= 0.001f;
                            float y3 = gameObject2.transform.localScale.y * Convert.ToSingle(array3[3]);
                            float z2 = gameObject2.transform.localScale.z * Convert.ToSingle(array3[4]);
                            gameObject2.transform.localScale = new Vector3(num2, y3, z2);
                            gameObject2.name = string.Concat(new string[]
                            {
                                array3[0],
                                ",",
                                array3[1],
                                ",",
                                array3[2],
                                ",",
                                array3[3],
                                ",",
                                array3[4]
                            });
                        }
                        else if (array3[1].StartsWith("region"))
                        {
                            float num3 = gameObject2.transform.localScale.x * Convert.ToSingle(array3[3]);
                            num3 -= 0.001f;
                            float y4 = gameObject2.transform.localScale.y * Convert.ToSingle(array3[4]);
                            float z3 = gameObject2.transform.localScale.z * Convert.ToSingle(array3[5]);
                            gameObject2.transform.localScale = new Vector3(num3, y4, z3);
                            gameObject2.name = string.Concat(new string[]
                            {
                                array3[0],
                                ",",
                                array3[1],
                                ",",
                                array3[2],
                                ",",
                                array3[3],
                                ",",
                                array3[4],
                                ",",
                                array3[5]
                            });
                        }
                    }
                    else if (array3[0].StartsWith("racing"))
                    {
                        float num4 = gameObject2.transform.localScale.x * Convert.ToSingle(array3[2]);
                        num4 -= 0.001f;
                        float y5 = gameObject2.transform.localScale.y * Convert.ToSingle(array3[3]);
                        float z4 = gameObject2.transform.localScale.z * Convert.ToSingle(array3[4]);
                        gameObject2.transform.localScale = new Vector3(num4, y5, z4);
                        gameObject2.name = string.Concat(new string[]
                        {
                            array3[0],
                            ",",
                            array3[1],
                            ",",
                            array3[2],
                            ",",
                            array3[3],
                            ",",
                            array3[4]
                        });
                    }
                    else if (array3[0].StartsWith("photon") && !array3[1].StartsWith("Cannon"))
                    {
                        gameObject2.name = string.Concat(new string[]
                        {
                            array3[0],
                            ",",
                            array3[1],
                            ",",
                            array3[2],
                            ",",
                            array3[3]
                        });
                    }
                    else
                    {
                        gameObject2.name = array3[0] + "," + array3[1];
                    }
                    FengGameManagerMKII.linkHash[3].Add(gameObject2.GetInstanceID(), array2[j]);
                }
                else if (array3[0].StartsWith("map") && array3[1].StartsWith("disablebounds"))
                {
                    FengGameManagerMKII.settings[186] = 1;
                    if (!FengGameManagerMKII.linkHash[3].ContainsKey("mapbounds"))
                    {
                        FengGameManagerMKII.linkHash[3].Add("mapbounds", "map,disablebounds");
                    }
                }
            }
            FengGameManagerMKII.instance.unloadAssets();
            FengGameManagerMKII.settings[77] = string.Empty;
        }

        public static void Update()
        {
            if ((int)FengGameManagerMKII.settings[64] >= 100)
            {
                if (Input.GetKeyDown(KeyCode.Comma))
                {
                    string name2 = FengGameManagerMKII.instance.selectedObj.name;
                    CGTools.log(FengGameManagerMKII.instance.selectedObj.name);
                    FengGameManagerMKII.linkHash[3].Add(FengGameManagerMKII.instance.selectedObj.GetInstanceID(), string.Concat(new string[]
                    {
                        FengGameManagerMKII.instance.selectedObj.name,
                        ",",
                        Convert.ToString(FengGameManagerMKII.instance.selectedObj.transform.position.x),
                        ",",
                        Convert.ToString(FengGameManagerMKII.instance.selectedObj.transform.position.y),
                        ",",
                        Convert.ToString(FengGameManagerMKII.instance.selectedObj.transform.position.z),
                        ",",
                        Convert.ToString(FengGameManagerMKII.instance.selectedObj.transform.rotation.x),
                        ",",
                        Convert.ToString(FengGameManagerMKII.instance.selectedObj.transform.rotation.y),
                        ",",
                        Convert.ToString(FengGameManagerMKII.instance.selectedObj.transform.rotation.z),
                        ",",
                        Convert.ToString(FengGameManagerMKII.instance.selectedObj.transform.rotation.w)
                    }));
                    FengGameManagerMKII.instance.selectedObj = (GameObject)UnityEngine.Object.Instantiate(FengGameManagerMKII.instance.selectedObj);
                    FengGameManagerMKII.instance.selectedObj.name = name2;
                }
            }
        }

        public static bool toggleLevelsDropDown;

        public static Color lockIndicatorColor;

        public static Color backgroundColor;

        public static bool skipTreeDeleting;

        public static bool toggleLevelSaveDialoge;

        public static bool toggleLevelLoadDialoge;

        private static int LoadLevelDialogePage;

        public static string LevelSaveName = "";

        private static float levelExistsTimer = 0f;
    }

    public class CGLog
    {
        public static void log(string message)
        {
            fullLog.Add(new logMessage(message));
            logTimer = Time.time + waitTime;
        }

        public static void log(string message, Color color)
        {
            fullLog.Add(new logMessage(message, color));
            logTimer = Time.time + waitTime;
        }

        public static void log(string message, int severity)
        {
            fullLog.Add(new logMessage(message, severity));
            logTimer = Time.time + waitTime;
        }

        public static void log(string message, Color color, int severity)
        {
            fullLog.Add(new logMessage(message, color, severity));
            logTimer = Time.time + waitTime;
        }

        public static void onGUI()
        {
            if (Time.time <= logTimer || showLogGui)
            {
                int num = Math.Min(15, fullLog.Count);
                for (int i = 0; i < num; i++)
                {
                    Rect position = new Rect((float)Screen.width - width, (float)(i * 25) + 50f, width, 25f);
                    GUI.DrawTexture(position, LogBackground);
                    GUI.Label(position, fullLog[fullLog.Count - num + i].message);
                }
            }
            
            if (trackedValues.Keys.Count > 0)
            {
                string[] keys = new string[trackedValues.Keys.Count];
                trackedValues.Keys.CopyTo(keys, 0);
                for (int i = 0; i < keys.Length; i++)
                {
                    if (trackedValueTimers[keys[i]] <= Time.time)
                    {
                        trackedValues.Remove(keys[i]);
                        trackedValueTimers.Remove(keys[i]);
                    }
                    Rect keyPos = new Rect((float)Screen.width - trackedWidth - (trackedWidth * i), 0f, trackedWidth, 30f);
                    Rect ValuePos = new Rect((float)Screen.width - trackedWidth - (trackedWidth * i), 30f, trackedWidth, 20f);
                    GUI.DrawTexture(keyPos, LogBackground);
                    GUI.DrawTexture(ValuePos, TrackedBackground);
                    GUI.Label(keyPos, keys[i], CGSettingsMenu.selectedLevelPathStyle);
                    GUI.Label(ValuePos, trackedValues[keys[i]]);
                }
            }
        }

        public static void clear()
        {
            fullLog.Clear();
        }

        public static void track(string key, string value)
        {
            trackedValues[key] = value;
            trackedValueTimers[key] = Time.time + 0.5f;
        }

        public static void FullLogGUI()
        {
        }

        public static void start()
        {
            LogBackground = FlatUI.FlatColorTexture(new Color(0.2f, 0.2f, 0.2f, 0.6f));
            TrackedBackground = FlatUI.FlatColorTexture(new Color(0.5f, 0.0f, 0.0f, 0.6f));
        }

        public static List<logMessage> fullLog = new List<logMessage>();

        public static float logTimer = 0f;

        public static bool showLogGui;

        public static Dictionary<string, string> trackedValues = new Dictionary<string, string>();

        public static Dictionary<string, float> trackedValueTimers = new Dictionary<string, float>();

        private static Texture2D LogBackground;
        private static Texture2D TrackedBackground;

        public static float waitTime = 5f;

        public static float trackedWidth = 150f;

        public static float width = 600f;
    }

    public static class CGPrefs
    {
        static CGPrefs()
        {
            preferences = new Dictionary<string, Dictionary<string, string>>();
        }

        public static void Set(string title, string key, string value)
        {
            if (preferences.ContainsKey(title))
            {
                if (preferences[title].ContainsKey(key))
                {
                    preferences[title][key] = value;
                }
                else
                {
                    preferences[title].Add(key, value);
                }
                return;
            }
            preferences.Add(title, new Dictionary<string, string>());
            preferences[title].Add(key, value);
        }

        public static string Get(string title, string key)
        {
            if (preferences.ContainsKey(title) && preferences[title].ContainsKey(key))
            {
                return preferences[title][key];
            }
            return "";
        }

        public static void SaveConfigFile()
        {
            CGSettingsMenu.saveConfig();
            try
            {
                List<string> list = new List<string>();
                list.Add("Custom Games Mod Preferences");
                list.Add("");
                list.Add("Contains all settings for only the CGMods additions");
                list.Add("____________________________________________________");
                if (preferences.Keys.Count > 0)
                {
                    string[] array = new string[preferences.Keys.Count];
                    preferences.Keys.CopyTo(array, 0);
                    foreach (string text in array)
                    {
                        list.Add("#" + text);
                        list.Add("{");
                        string[] array3 = new string[preferences[text].Keys.Count];
                        preferences[text].Keys.CopyTo(array3, 0);
                        foreach (string text2 in array3)
                        {
                            list.Add("\t" + text2 + ";" + Get(text, text2));
                        }
                        list.Add("}");
                    }
                }
                File.WriteAllLines(filePath, list.ToArray());
                CGLog.log("Saved preferences to " + filePath);
            }
            catch (Exception ex)
            {
                CGLog.log("Saving Error!  " + ex.Message);
            }
        }

        public static void LoadConfigFile()
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    CGLog.log("Config file does not exist, creating new one");
                    SaveConfigFile();
                }
                else
                {
                    string[] array = File.ReadAllLines(filePath);
                    string title = "";
                    bool flag = true;
                    List<string> list = new List<string>();
                    bool flag2 = false;
                    for (int i = 0; i < array.Length; i++)
                    {
                        if (flag)
                        {
                            if (array[i].StartsWith("#"))
                            {
                                title = array[i].Substring(1);
                                flag = false;
                            }
                        }
                        else if (flag2)
                        {
                            if (array[i].StartsWith("}"))
                            {
                                foreach (string text in list)
                                {
                                    string[] array2 = text.Split(new char[]
                                    {
                                        ';'
                                    }, 2);
                                    Set(title, array2[0], array2[1]);
                                }
                                list.Clear();
                                flag2 = false;
                                title = "";
                                flag = true;
                            }
                            else if (array[i].Contains(";"))
                            {
                                list.Add(array[i].Substring(1));
                            }
                        }
                        else if (array[i].StartsWith("#"))
                        {
                            CGLog.log("key is already defined and did not finish loading! {" + array[i] + "}");
                        }
                        else if (array[i].StartsWith("{"))
                        {
                            flag2 = true;
                        }
                    }
                }
                CGSettingsMenu.LoadConfig();
                CGLog.log("Loaded preferences from " + filePath);
            }
            catch (Exception ex)
            {
                CGLog.log("Loading Error!  " + ex.Message);
            }
        }

        public static Dictionary<string, Dictionary<string, string>> preferences;

        private static readonly string filePath = "CGConfig\\Settings.txt";
    }

    public class CGSettingsMenu
    {
        public static void LevelManager()
        {
            float num = (float)Screen.width / 2f - 350f;
            float num2 = (float)Screen.height / 2f - 275f;
            if (GUI.Button(new Rect(num + 200f, num2 + 46f, 150f, 26f), "Level Manager"))
            {
                ShowLevelManager = false;
            }
            if (GUI.Button(new Rect(num + 350f, num2 + 46f, 150f, 26f), "CG Level Manager"))
            {
                ShowLevelManager = true;
            }
            float num3 = num + 50f;
            float num4 = num2 + 85f;
            int num5 = 14;
            Rect[] array = new Rect[num5];
            for (int i = 0; i < num5; i++)
            {
                array[i] = new Rect(num3 + 5f, num4 + (float)i * 30f, 550f, 30f);
            }
            if (useLevelSelector)
            {
                updateLevels();
                if (levels.Length != 0)
                {
                    int num6 = levelSelectorPage * num5;
                    int num7 = Math.Min(num6 + num5 - 1, levels.Length - 1);
                    Math.Min(num6 + num5 - 1, levels.Length - 1);
                    for (int j = num6; j <= num7; j++)
                    {
                        if (GUI.Button(array[j - num5 * levelSelectorPage], levels[j]))
                        {
                            string[] array2 = File.ReadAllLines(levels[j]);
                            string str = "";
                            List<string> list = new List<string>();
                            foreach (string text in array2)
                            {
                                str += text;
                                str += Environment.NewLine;
                                list.Add(text.TrimEnd(new char[]
                                {
                                    ';'
                                }));
                            }
                            levelTextArea = str;
                            selectedLevelPath = levels[j];
                            selectedLevel = list;
                            useLevelSelector = false;
                        }
                    }
                    if (levels.Length > num5)
                    {
                        if (levelSelectorPage > 0 && GUI.Button(new Rect(num + 150f, num2 + 505f, 50f, 30f), "<"))
                        {
                            levelSelectorPage--;
                        }
                        float num8 = Mathf.Ceil((float)levels.Length / (float)num5);
                        if ((float)levelSelectorPage < num8 - 1f && GUI.Button(new Rect(num + 200f, num2 + 505f, 50f, 30f), ">"))
                        {
                            levelSelectorPage++;
                            return;
                        }
                    }
                }
            }
            else
            {
                CGCustomLevelManager.useCustomLevel = GUI.Toggle(array[0], CGCustomLevelManager.useCustomLevel, "Enable CG Level Manager");
                GUI.Label(FlatUI.split(array[1], 0, 2), "Level to use");
                if (GUI.Button(FlatUI.split(array[1], 1, 2), "Select"))
                {
                    useLevelSelector = true;
                }
                GUI.Label(FlatUI.split(array[2], 0, 4), "Selected Level:", selectedLevelPathStyle);
                GUI.Label(FlatUI.split(array[2], 1, 4, 3), selectedLevelPath, selectedLevelPathStyle);
                levelTextArea = GUI.TextArea(new Rect(array[3].x, array[3].y, array[3].width, array[3].height * 4f), levelTextArea);
                GUI.Label(FlatUI.split(array[7], 0, 4), "Current Level:", levelPathStyle);
                GUI.Label(FlatUI.split(array[7], 1, 4, 3), appliedLevelPath, levelPathStyle);
                if (GUI.Button(FlatUI.split(array[8], 0, 3), "Preview"))
                {
                    CGCustomLevelManager.previewLevel(selectedLevel.ToArray());
                }
                if (GUI.Button(FlatUI.split(array[8], 1, 3), "Reset Preview"))
                {
                    CGCustomLevelManager.previewLevel(new string[0]);
                }
                if (GUI.Button(FlatUI.split(array[8], 2, 3), "Restart and apply"))
                {
                    CGCustomLevelManager.ChangeMap(selectedLevel.ToArray());
                    appliedLevelPath = selectedLevelPath;
                }
                GUI.Label(FlatUI.split(array[9], 0, 4, 3), "Lines per RPC");
                linesPerRPCTextBox = GUI.TextArea(FlatUI.split(array[9], 3, 4), linesPerRPCTextBox);
                GUI.Label(array[10], "How much data is sent at once, if you set this too high you could be disconnected");
                GUI.Label(FlatUI.split(array[11], 0, 4, 3), "RPC Delay (in seconds)");
                RPCDelayTextBox = GUI.TextArea(FlatUI.split(array[11], 3, 4), RPCDelayTextBox);
                GUI.Label(array[12], "How much time to wait between RPC's, if you set this too low you could be disconnected");
                if (GUI.Button(FlatUI.split(array[13], 0, 3), "Apply Settings"))
                {
                    CGPrefs.Set("CGCustomLevelManager", "linesPerRPC", linesPerRPCTextBox);
                    CGPrefs.Set("CGCustomLevelManager", "RPCDelay", RPCDelayTextBox);
                    CGCustomLevelManager.sendsPerInterval = Convert.ToInt32(linesPerRPCTextBox);
                    CGCustomLevelManager.sendWaitInterval = Convert.ToSingle(RPCDelayTextBox);
                }
            }
        }

        public static void DrawPlayerBox(Rect pos, PhotonPlayer player)
        {
            int team = (int)player.customProperties[PhotonPlayerProperty.RCteam];
            FlatUI.Box(pos, playerBoxBackground);
            GUI.Label(new Rect(pos.x,pos.y,pos.width-90f,pos.height), ((string)player.customProperties[PhotonPlayerProperty.name]).hexColor(), levelPathStyle);
            
            GUI.DrawTexture(new Rect(pos.x + pos.width - 90f, pos.y, 30f, pos.height), cyan);
            GUI.DrawTexture(new Rect(pos.x + pos.width - 60f, pos.y, 30f, pos.height), magenta);
            GUI.DrawTexture(new Rect(pos.x + pos.width - 30f, pos.y, 30f, pos.height), white);

            if (GUI.Button(new Rect(pos.x + pos.width - 90f, pos.y, 30f, pos.height), "C"))
            {
                CGSoccer.setTeam(player, 1);
                RefreshTeamsLists();                
            }
            if (GUI.Button(new Rect(pos.x + pos.width - 60f, pos.y, 30f, pos.height), "M"))
            {
                CGSoccer.setTeam(player, 2);
                RefreshTeamsLists();
            }
            if (GUI.Button(new Rect(pos.x + pos.width - 30f, pos.y, 30f, pos.height), "N"))
            {
                CGSoccer.setTeam(player, 0);
                RefreshTeamsLists();
            }
        }

        private static void RefreshTeamsLists()
        {
            cyanPlayers.Clear();
            magentaPlayers.Clear();
            noTeamPlayers.Clear();
            foreach (PhotonPlayer photonPlayer in PhotonNetwork.playerList)
            {
                if ((int)photonPlayer.customProperties[PhotonPlayerProperty.RCteam] == 1)
                {
                    cyanPlayers.Add(photonPlayer);
                }
                else if ((int)photonPlayer.customProperties[PhotonPlayerProperty.RCteam] == 2)
                {
                    magentaPlayers.Add(photonPlayer);
                }
                else if ((int)photonPlayer.customProperties[PhotonPlayerProperty.RCteam] == 0)
                {
                    noTeamPlayers.Add(photonPlayer);
                }
            }
        }

        public static string CheckRegions()
        {
            bool hasBallSpawn = false;
            bool hasGoalRed = false;
            bool hasGoalBlue = false;
            string result = "";
            foreach (string s in FengGameManagerMKII.RCRegions.Keys)
            {
                if (s == "BallSpawn") hasBallSpawn = true;
                if (s == "GoalRed") hasGoalRed = true;
                if (s == "GoalBlue") hasGoalBlue = true;
            }
            if (!hasBallSpawn)
            {
                result += "Missing Region \"BallSpawn\"\n";
            }
            if (!hasGoalRed)
            {
                result += "Missing Region \"GoalRed\"\n";
            }
            if (!hasGoalBlue)
            {
                result += "Missing Region \"GoalBlue\"\n";
            }
            if (result != "")
            {
                result = "Unable to start game, missing regions:\n" + result;
            }
            return result;

        }

        public static void TeamManager()
        {
            float left = (float)Screen.width / 2f - 390f;
            float top = (float)Screen.height / 2f - 190f;
            float rowsLeft = left + 50f;
            float rowsTop = top + 60f;
            int ItemsPerPage = 14;
            Rect[] array = new Rect[ItemsPerPage];
            for (int i = 0; i < ItemsPerPage; i++)
            {
                array[i] = new Rect(rowsLeft + 5f, rowsTop + (float)i * 30f, 670f, 30f);
            }
            Rect title = new Rect(left, top, 600f, 30f);
            Rect teams = new Rect(left, top + 30f, 670f, 30f);
            GUI.Label(title, "Team Manager", titleStyle);
            GUI.Label(FlatUI.split(teams, 0, 18, 6), "Cyan", cyanStyle);
            GUI.Label(FlatUI.split(teams, 6, 18, 6), "Magenta", magentaStyle);
            GUI.Label(FlatUI.split(teams, 12, 18, 6), "NoTeam", levelPathStyle);


            if (GUI.Button(new Rect(left + 500f, top, 80f,30f), "Refresh"))
            {
                RefreshTeamsLists();
            }
            if (GUI.Button(new Rect(left + 580f, top, 80f, 30f), "Return"))
            {
                showTeamManager = false;
            }

            
            if (cyanPlayers.Count > 0)
            {
                int start = cyanPlayerPage * ItemsPerPage;
                int end = Math.Min(start + ItemsPerPage - 1, cyanPlayers.Count - 1);
                Math.Min(start + ItemsPerPage - 1, cyanPlayers.Count - 1);
                for (int j = start; j <= end; j++)
                {
                    DrawPlayerBox(FlatUI.split(array[j], 0, 18, 6),cyanPlayers[j]);
                }
                if (cyanPlayers.Count > ItemsPerPage)
                {
                    if (cyanPlayerPage > 0 && GUI.Button(new Rect(left + 150f, top + 505f, 50f, 30f), "<"))
                    {
                        cyanPlayerPage--;
                    }
                    float num8 = Mathf.Ceil((float)levels.Length / (float)ItemsPerPage);
                    if ((float)cyanPlayerPage < num8 - 1f && GUI.Button(new Rect(left + 200f, top + 505f, 50f, 30f), ">"))
                    {
                        cyanPlayerPage++;
                        return;
                    }
                }
            }
            if (magentaPlayers.Count > 0)
            {
                int start = magentaPlayerPage * ItemsPerPage;
                int end = Math.Min(start + ItemsPerPage - 1, magentaPlayers.Count - 1);
                Math.Min(start + ItemsPerPage - 1, magentaPlayers.Count - 1);
                for (int j = start; j <= end; j++)
                {
                    DrawPlayerBox(FlatUI.split(array[j], 6, 18, 6), magentaPlayers[j]);
                }
                if (magentaPlayers.Count > ItemsPerPage)
                {
                    if (magentaPlayerPage > 0 && GUI.Button(new Rect(left + 150f, top + 505f, 50f, 30f), "<"))
                    {
                        magentaPlayerPage--;
                    }
                    float num8 = Mathf.Ceil((float)levels.Length / (float)ItemsPerPage);
                    if ((float)magentaPlayerPage < num8 - 1f && GUI.Button(new Rect(left + 200f, top + 505f, 50f, 30f), ">"))
                    {
                        magentaPlayerPage++;
                        return;
                    }
                }
            }
            if (noTeamPlayers.Count > 0)
            {
                int start = noTeamPlayerPage * ItemsPerPage;
                int end = Math.Min(start + ItemsPerPage - 1, noTeamPlayers.Count - 1);
                Math.Min(start + ItemsPerPage - 1, noTeamPlayers.Count - 1);
                for (int j = start; j <= end; j++)
                {
                    DrawPlayerBox(FlatUI.split(array[j], 12, 18, 6), noTeamPlayers[j]);
                }
                if (noTeamPlayers.Count > ItemsPerPage)
                {
                    if (noTeamPlayerPage > 0 && GUI.Button(new Rect(left + 150f, top + 505f, 50f, 30f), "<"))
                    {
                        noTeamPlayerPage--;
                    }
                    float num8 = Mathf.Ceil((float)levels.Length / (float)ItemsPerPage);
                    if ((float)noTeamPlayerPage < num8 - 1f && GUI.Button(new Rect(left + 200f, top + 505f, 50f, 30f), ">"))
                    {
                        noTeamPlayerPage++;
                        return;
                    }
                }
            }
        }

        public static void SoccerGameMode()
        {
            if (showTeamManager)
            {
                TeamManager();
                return;
            }
            float num = (float)Screen.width / 2f - 350f;
            float num2 = (float)Screen.height / 2f - 275f;
            float num3 = num + 50f;
            float num4 = num2 + 85f;
            int num5 = 14;
            Rect[] array = new Rect[num5];
            for (int i = 0; i < num5; i++)
            {
                array[i] = new Rect(num3 + 5f, num4 + (float)i * 30f, 550f, 30f);
            }
            if (!CGSoccer.doGameMode)
            {
                if (GUI.Button(FlatUI.split(array[0], 0, 3), "Start Gamemode"))
                {
                    checkRegionsResponse = CheckRegions();
                    if (checkRegionsResponse == "")
                    {
                        CGSoccer.doGameMode = true;
                        RCSettings.gameType = 4;
                        FengGameManagerMKII.settings[216] = 0;
                        FengGameManagerMKII.settings[193] = 2;
                        CGSoccer.needsNewRound = true;
                        FengGameManagerMKII.instance.restartGame2(false);
                        return;
                    }
                }
                GUI.Label(new Rect(array[9].x, array[9].y, 350f, 90f), checkRegionsResponse, selectedLevelPathStyle);
            }
            else
            {
                if (GUI.Button(FlatUI.split(array[0], 0, 3), "End Gamemode"))
                {
                    CGSoccer.doGameMode = false;
                    PhotonNetwork.Destroy(CGSoccer.ball);
                    PhotonNetwork.Destroy(CGSoccer.ballMapIndicatorTitan);
                    PhotonNetwork.Destroy(CGSoccer.bombTrail);
                }
                if (GUI.Button(FlatUI.split(array[1], 0, 3), "Restart Game"))
                {
                    CGSoccer.needsNewRound = true;
                    FengGameManagerMKII.instance.restartGame2(false);
                }
                if (GUI.Button(FlatUI.split(array[0], 2, 3), "Reset Score"))
                {
                    CGSoccer.redPoints = 0;
                    CGSoccer.bluePoints = 0;
                    PhotonPlayer[] playerList = PhotonNetwork.playerList;
                    for (int j = 0; j < playerList.Length; j++)
                    {
                        playerList[j].SetCustomProperties(new Hashtable
                        {
                            {
                                PhotonPlayerProperty.kills,
                                0
                            }
                        });
                    }
                    object[] parameters = new object[]
                    {
                    "<color=#FF8000>New Game!</color>",
                    ""
                    };
                    FengGameManagerMKII.instance.photonView.RPC("Chat", PhotonTargets.All, parameters);
                    FengGameManagerMKII.instance.restartRC();
                    PhotonNetwork.Destroy(CGSoccer.ball);
                    PhotonNetwork.Destroy(CGSoccer.bombTrail);
                    PhotonNetwork.Destroy(CGSoccer.ballMapIndicatorTitan);
                    CGSoccer.needsNewRound = true;
                    CGSoccer.needsRestart = false;
                    CGSoccer.timeUntilRestart = 0f;
                }
                if (GUI.Button(FlatUI.split(array[3], 3, 6, 2), "Reset Ball"))
                {
                    CGSoccer.ResetBall();
                }
            }
            


            GUI.Label(FlatUI.split(array[2], 0, 2), "Ball Properties", levelPathStyle);
            GUI.Label(FlatUI.split(array[3], 0, 8, 2), "Radius: ");
            ballRadiusTextBox = GUI.TextArea(FlatUI.split(array[3], 2, 8), ballRadiusTextBox);
            GUI.Label(FlatUI.split(array[4], 0, 8, 2), "Max Speed: ");
            ballMaxSpeedTextBox = GUI.TextArea(FlatUI.split(array[4], 2, 8), ballMaxSpeedTextBox);
            GUI.Label(FlatUI.split(array[5], 0, 8, 2), "Force Multiplier: ");
            ballForceMultiplierTextBox = GUI.TextArea(FlatUI.split(array[5], 2, 8), ballForceMultiplierTextBox);
            GUI.Label(FlatUI.split(array[6], 0, 8, 2), "Friction: ");
            ballFrictionTextBox = GUI.TextArea(FlatUI.split(array[6], 2, 8), ballFrictionTextBox);
            GUI.Label(FlatUI.split(array[7], 0, 8, 2), "Bounciness: ");
            ballBouncinessTextBox = GUI.TextArea(FlatUI.split(array[7], 2, 8), ballBouncinessTextBox);
            GUI.Label(FlatUI.split(array[2], 1, 2), "Game Properties", levelPathStyle);
            if (GUI.Button(FlatUI.split(array[4], 3, 6, 2), "Manage Teams"))
            {
                showTeamManager = true;
            }
            CGSoccer.doJoinCard = GUI.Toggle(FlatUI.split(array[5], 3, 6, 3), CGSoccer.doJoinCard, " Send Welcome Message");
            GUI.Label(FlatUI.split(array[6], 3, 6, 3), "Goal Explosion Force");
            goalExplosionForceTextBox = GUI.TextArea(FlatUI.split(array[6], 5, 6), goalExplosionForceTextBox);
            GUI.Label(FlatUI.split(array[7], 3, 6, 2), "Titan Height");
            titanHeightTextBox = GUI.TextArea(FlatUI.split(array[7], 5, 6), titanHeightTextBox);
            GUI.Label(FlatUI.split(array[8], 3, 6, 2), "Bomb Height");
            bombHeightTextBox = GUI.TextArea(FlatUI.split(array[8], 5, 6), bombHeightTextBox);
            disallowHooksCheckBox = GUI.Toggle(FlatUI.split(array[9], 3, 6), disallowHooksCheckBox, " Disallow Hooks");
            if (GUI.Button(FlatUI.split(array[10], 4,6,2), "Apply"))
            {
                ApplySettings();
            }
            
        }
        
        public static void ApplySettings()
        {
            CGSoccer.ballRadius = float.Parse(ballRadiusTextBox);
            CGSoccer.BallForceMaxSpeed = float.Parse(ballMaxSpeedTextBox);
            CGSoccer.BallForceMultiplier = float.Parse(ballForceMultiplierTextBox);
            float friction = float.Parse(ballFrictionTextBox);
            float bounciness = float.Parse(ballBouncinessTextBox);
            CGSoccer.GoalExplosionForce = float.Parse(goalExplosionForceTextBox);
            CGSoccer.titanHeight = float.Parse(titanHeightTextBox);
            CGSoccer.bombIndicatorHeight = float.Parse(bombHeightTextBox);
            CGSoccer.PhysicsSettings[0] = bounciness;
            CGSoccer.PhysicsSettings[1] = friction;
            CGSoccer.PhysicsSettings[2] = friction;
            CGSoccer.PhysicsSettings[3] = friction;
            CGSoccer.PhysicsSettings[4] = friction;
            if (CGSoccer.ball != null) CGSoccer.SetBallRigidbodySettings();
            CGSoccer.disallowHooks = disallowHooksCheckBox;
        }

        public static void updateLevels()
        {
            if (CGTools.timer(ref levelUpdateTimer, 1f) && Directory.Exists("levels/"))
            {
                string[] files = Directory.GetFiles("levels/");
                List<string> list = new List<string>();
                foreach (string text in files)
                {
                    if (text.Split(new char[]
                    {
                        '.'
                    })[1] == "txt")
                    {
                        list.Add(text);
                    }
                }
                levels = list.ToArray();
            }
        }

        public static void Start()
        {
            levelPathStyle.alignment = TextAnchor.MiddleCenter;
            levelPathStyle.fontStyle = FontStyle.Bold;
            levelPathStyle.fontSize = 16;
            levelPathStyle.normal.textColor = Color.white;
            selectedLevelPathStyle.alignment = TextAnchor.MiddleCenter;
            selectedLevelPathStyle.fontStyle = FontStyle.Bold;
            selectedLevelPathStyle.fontSize = 16;
            selectedLevelPathStyle.normal.textColor = Color.yellow;
            cyanStyle.alignment = TextAnchor.MiddleCenter;
            cyanStyle.fontStyle = FontStyle.Bold;
            cyanStyle.fontSize = 16;
            cyanStyle.normal.textColor = Color.cyan;
            magentaStyle.alignment = TextAnchor.MiddleCenter;
            magentaStyle.fontStyle = FontStyle.Bold;
            magentaStyle.fontSize = 16;
            magentaStyle.normal.textColor = Color.magenta;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.fontSize = 24;
            titleStyle.normal.textColor = Color.white;
            playerBoxBackground = FlatUI.FlatColorTexture(new Color(0.1f, 0.1f, 0.1f, 1f));
            magenta = FlatUI.FlatColorTexture(new Color(1f, 0f, 1f, 1f));
            cyan = FlatUI.FlatColorTexture(new Color(0f, 1f, 1f, 1f));
            white = FlatUI.FlatColorTexture(new Color(1f, 1f, 1f, 1f));
            transparent = FlatUI.FlatColorTexture(new Color(0f, 0f, 0f, 0f));
            playerTeamButtons.border = new RectOffset(0,0,0,0);
            playerTeamButtons.normal.background = transparent;
        }
    
        public static void saveConfig()
        {
            CGPrefs.Set("CGCustomLevelManager", "linesPerRPC", linesPerRPCTextBox);
            CGPrefs.Set("CGCustomLevelManager", "RPCDelay", RPCDelayTextBox);
            CGPrefs.Set("CGSoccer", "ballRadius", ballRadiusTextBox);
            CGPrefs.Set("CGSoccer", "ballMaxSpeed", ballMaxSpeedTextBox);
            CGPrefs.Set("CGSoccer", "ballForceMultiplier", ballForceMultiplierTextBox);
            CGPrefs.Set("CGSoccer", "ballFriction", ballFrictionTextBox);
            CGPrefs.Set("CGSoccer", "ballBounciness", ballBouncinessTextBox);
            CGPrefs.Set("CGSoccer", "goalExplosionForce", goalExplosionForceTextBox);
            CGPrefs.Set("CGSoccer", "titanHeight", titanHeightTextBox);
            CGPrefs.Set("CGSoccer", "bombHeight", bombHeightTextBox);
            CGPrefs.Set("CGSoccer", "doJoinCard", CGSoccer.doJoinCard.ToString());
            CGPrefs.Set("CGSoccer", "disallowHooks", CGSoccer.disallowHooks.ToString());
        }

        public static void LoadConfig()
        {
            linesPerRPCTextBox = CGPrefs.Get("CGCustomLevelManager", "linesPerRPC");
            RPCDelayTextBox = CGPrefs.Get("CGCustomLevelManager", "RPCDelay");
            ballRadiusTextBox = CGPrefs.Get("CGSoccer", "ballRadius");
            ballMaxSpeedTextBox = CGPrefs.Get("CGSoccer", "ballMaxSpeed");
            ballForceMultiplierTextBox = CGPrefs.Get("CGSoccer", "ballForceMultiplier");
            ballFrictionTextBox = CGPrefs.Get("CGSoccer", "ballFriction");
            ballBouncinessTextBox = CGPrefs.Get("CGSoccer", "ballBounciness");
            goalExplosionForceTextBox = CGPrefs.Get("CGSoccer", "goalExplosionForce");
            titanHeightTextBox = CGPrefs.Get("CGSoccer", "titanHeight");
            bombHeightTextBox = CGPrefs.Get("CGSoccer", "bombHeight");
            if (!bool.TryParse(CGPrefs.Get("CGSoccer", "doJoinCard"), out CGSoccer.doJoinCard))
            {
                CGSoccer.doJoinCard = true;
            }
            if (!bool.TryParse(CGPrefs.Get("CGSoccer", "disallowHooks"), out CGSoccer.disallowHooks))
            {
                disallowHooksCheckBox = false;
            }
            if (File.Exists("CGConfig\\JoinMessage.txt"))
            {
                CGSoccer.joinMessage = File.ReadAllText("CGConfig\\JoinMessage.txt");
            }
            ApplySettings();

        }

        private static bool disallowHooksCheckBox = false;
        private static string checkRegionsResponse = "";
        private static float recalculatePlayerListsTimer = 0f;
        private static List<PhotonPlayer> cyanPlayers = new List<PhotonPlayer>();
        private static List<PhotonPlayer> magentaPlayers = new List<PhotonPlayer>();
        private static List<PhotonPlayer> noTeamPlayers = new List<PhotonPlayer>();
        private static int cyanPlayerPage = 0;
        private static int magentaPlayerPage = 0;
        private static int noTeamPlayerPage = 0;
        public static bool ShowLevelManager = false;
        public static string levelTextArea = "";
        private static bool useLevelSelector = false;
        private static float levelUpdateTimer = 0f;
        public static string[] levels = new string[0];
        private static int levelSelectorPage = 0;
        private static string selectedLevelPath = "";
        public static GUIStyle levelPathStyle = new GUIStyle();
        public static GUIStyle cyanStyle = new GUIStyle();
        public static GUIStyle magentaStyle = new GUIStyle();
        public static GUIStyle titleStyle = new GUIStyle();
        public static List<string> selectedLevel = new List<string>();
        public static GUIStyle selectedLevelPathStyle = new GUIStyle();
        public static GUIStyle playerTeamButtons = new GUIStyle();
        public static string linesPerRPCTextBox = "50";
        public static string RPCDelayTextBox = "1";
        public static string appliedLevelPath = "None";
        public static bool showSoccer = false;
        public static bool showTeamManager = false;
        private static Texture2D playerBoxBackground;
        public static Texture2D magenta;
        public static Texture2D cyan;
        public static Texture2D white;
        public static Texture2D transparent;
        private static string ballRadiusTextBox = "";
        private static string ballMaxSpeedTextBox = "";
        private static string ballForceMultiplierTextBox = "";
        private static string ballFrictionTextBox = "";
        private static string ballBouncinessTextBox = "";
        private static string titanHeightTextBox = "";
        private static string bombHeightTextBox = "";
        private static string goalExplosionForceTextBox = "";
    }

    public static class CGSoccer
    {
        public static void update()
        {
            if (doGameMode)
            {
                if (!needsNewRound && !needsRestart)
                {
                    FixedUpdate();
                    if (ball != null)
                    {
                        if (CGTools.IsPointInRegion(ball.transform.position, (RCRegion)FengGameManagerMKII.RCRegions["GoalRed"]))
                        {
                            if (CGTools.timer(ref ScoreResetTimer, 5f))
                            {
                                OnGoalScored(false);
                                return;
                            }
                            return;
                        }
                        else if (CGTools.IsPointInRegion(ball.transform.position, (RCRegion)FengGameManagerMKII.RCRegions["GoalBlue"]) && CGTools.timer(ref ScoreResetTimer, 5f))
                        {
                            OnGoalScored(true);
                            return;
                        }
                    }
                    return;
                }
                if (needsRestart)
                {
                    if (timeUntilRestart > 0f)
                    {
                        timeUntilRestart -= Time.deltaTime;
                        return;
                    }
                    FengGameManagerMKII.instance.restartGame2(false);
                    needsRestart = false;
                }
                if (roundTimer > 0f)
                {
                    roundTimer -= Time.deltaTime;
                    return;
                }
                needsNewRound = false;
                if (!restartEachRound)
                {
                    roundTimer = 5f;
                    foreach (GameObject gameObject in GameObject.FindGameObjectsWithTag("Player"))
                    {
                        Vector3 vector = new Vector3(0f, 20f, 0f);
                        HERO component = gameObject.GetComponent<HERO>();
                        if (RCextensions.returnIntFromObject(component.photonView.owner.customProperties[PhotonPlayerProperty.RCteam]) == 1)
                        {
                            if (FengGameManagerMKII.instance.playerSpawnsC.Count > 0)
                            {
                                vector = FengGameManagerMKII.instance.playerSpawnsC[UnityEngine.Random.Range(0, FengGameManagerMKII.instance.playerSpawnsC.Count)];
                                component.photonView.RPC("moveToRPC", component.photonView.owner, new object[]
                                {
                                    vector.x,
                                    vector.y,
                                    vector.z
                                });
                                component.photonView.RPC("blowAway", component.photonView.owner, new object[]
                                {
                                    -0.5f * component.rigidbody.velocity
                                });
                            }
                        }
                        else if (RCextensions.returnIntFromObject(component.photonView.owner.customProperties[PhotonPlayerProperty.RCteam]) == 2 && FengGameManagerMKII.instance.playerSpawnsM.Count > 0)
                        {
                            vector = FengGameManagerMKII.instance.playerSpawnsM[UnityEngine.Random.Range(0, FengGameManagerMKII.instance.playerSpawnsM.Count)];
                            component.photonView.RPC("moveToRPC", component.photonView.owner, new object[]
                            {
                                vector.x,
                                vector.y,
                                vector.z
                            });
                            component.photonView.RPC("blowAway", component.photonView.owner, new object[]
                            {
                                -0.5f * component.rigidbody.velocity
                            });
                        }
                    }
                }
                else
                {
                    roundTimer = 1f;
                }
                SpawnBall();
            }
        }

        public static void OnGoalScored(bool TFRedBlue)
        {
            PhotonNetwork.Destroy(ball);
            PhotonNetwork.Destroy(bombTrail);
            PhotonNetwork.Destroy(ballMapIndicatorTitan);
            if (TFRedBlue)
            {
                GoalExplosion(((RCRegion)FengGameManagerMKII.RCRegions["GoalBlue"]).location);
            }
            else
            {
                GoalExplosion(((RCRegion)FengGameManagerMKII.RCRegions["GoalRed"]).location);
            }
            needsNewRound = true;
            AddPoint(TFRedBlue);
            AnnouncePlayerScored(TFRedBlue);
            needsRestart = true;
            timeUntilRestart = 8f;
        }

        public static void FixedUpdate()
        {
            if (ball != null && bombTrail != null && ballMapIndicatorTitan != null)
            {
                bombTrail.transform.position = new Vector3(ball.transform.position.x, bombIndicatorHeight, ball.transform.position.z);
                ballMapIndicatorTitan.transform.position = new Vector3(ball.transform.position.x, titanHeight, ball.transform.position.z);
            }
        }

        static CGSoccer()
        {
            anounceScore = true;
            timeUntilRestart = 0f;
            needsRestart = false;
            ball = null;
            redPoints = 0;
            bluePoints = 0;
            bombTrail = null;
            bombIndicatorHeight = 30f;
            ScoreResetTimer = 5f;
            needsNewRound = true;
            roundTimer = 5f;
            CustomPropertiesCooldown = new float[7];
            LastCyanToHitBall = null;
            LastMagentaToHitBall = null;
            restartEachRound = true;
            titanHeight = -500f;
            GoalExplosionForce = 100f;
            BallForceMultiplier = 1.5f;
            BallForceMaxSpeed = 300f;
            PhysicsSettings = new float[]
            {
                1f,
                0.2f,
                0.2f,
                0.2f,
                0.2f
            };
            ballRadius = 10f;
            ballSize = 5f;
        }

        public static void AddPoint(bool TFRedBlue)
        {
            if (TFRedBlue)
            {
                redPoints++;
                if (!useScoreKeepers)
                {
                    givePoint(true);
                }
                return;
            }
            bluePoints++;
            if (!useScoreKeepers)
            {
                givePoint(false);
                return;
            }
            EnsureScoreKeepers();
        }

        public static void EnsureScoreKeepers()
        {
            if (PhotonNetwork.playerList.Length < 2)
            {
                return;
            }
            bool flag = true;
            bool flag2 = true;
            foreach (PhotonPlayer photonPlayer in PhotonNetwork.playerList)
            {
                if (photonPlayer == scoreKeeperCyan && (int)photonPlayer.customProperties[PhotonPlayerProperty.RCteam] == 1)
                {
                    flag2 = false;
                    if ((int)photonPlayer.customProperties[PhotonPlayerProperty.kills] != bluePoints)
                    {
                        if (CGTools.timer(ref CustomPropertiesCooldown[0], 1f))
                        {
                            photonPlayer.SetCustomProperties(new Hashtable
                            {
                                {
                                    PhotonPlayerProperty.kills,
                                    bluePoints
                                }
                            });
                        }
                        else
                        {
                            CGTools.log("Too many Properties Changes Updating CyanKeeper Kills");
                        }
                    }
                }
                else if (photonPlayer == scoreKeeperMagenta && (int)photonPlayer.customProperties[PhotonPlayerProperty.RCteam] == 2)
                {
                    flag = false;
                    if ((int)photonPlayer.customProperties[PhotonPlayerProperty.kills] != redPoints)
                    {
                        if (CGTools.timer(ref CustomPropertiesCooldown[1], 1f))
                        {
                            photonPlayer.SetCustomProperties(new Hashtable
                            {
                                {
                                    PhotonPlayerProperty.kills,
                                    redPoints
                                }
                            });
                        }
                        else
                        {
                            CGTools.log("Too many Properties Changes Updating MagentaKeeper Kills");
                        }
                    }
                }
                else if ((int)photonPlayer.customProperties[PhotonPlayerProperty.kills] != 0)
                {
                    CGTools.log("player {" + photonPlayer.ID + "} has too many kills and is not scorekeeper");
                    if (CGTools.timer(ref CustomPropertiesCooldown[4], 1f))
                    {
                        photonPlayer.SetCustomProperties(new Hashtable
                        {
                            {
                                PhotonPlayerProperty.kills,
                                0
                            }
                        });
                    }
                    else
                    {
                        CGTools.log("Too many Properties Changes for player {" + photonPlayer.ID + "} who has too many kills");
                    }
                }
            }
            if (flag2)
            {
                CGTools.log("There is no Cyan ScoreKeeper");
                PhotonPlayer[] playerList2 = PhotonNetwork.playerList;
                int j = 0;
                while (j < playerList2.Length)
                {
                    PhotonPlayer photonPlayer2 = playerList2[j];
                    if ((int)photonPlayer2.customProperties[PhotonPlayerProperty.RCteam] == 1)
                    {
                        scoreKeeperCyan = photonPlayer2;
                        CGTools.log("Cyan ScoreKeeper is now " + photonPlayer2.ID.ToString());
                        if (CGTools.timer(ref CustomPropertiesCooldown[2], 1f))
                        {
                            photonPlayer2.SetCustomProperties(new Hashtable
                            {
                                {
                                    PhotonPlayerProperty.kills,
                                    bluePoints
                                }
                            });
                            return;
                        }
                        CGTools.log("Too many Properties Changes setting Cyan ScoreKeeper kills");
                        return;
                    }
                    else
                    {
                        j++;
                    }
                }
            }
            if (flag)
            {
                CGTools.log("There is no Magenta ScoreKeeper");
                PhotonPlayer[] playerList3 = PhotonNetwork.playerList;
                int k = 0;
                while (k < playerList3.Length)
                {
                    PhotonPlayer photonPlayer3 = playerList3[k];
                    if ((int)photonPlayer3.customProperties[PhotonPlayerProperty.RCteam] == 2)
                    {
                        scoreKeeperMagenta = photonPlayer3;
                        CGTools.log("Magenta ScoreKeeper is now " + photonPlayer3.ID.ToString());
                        if (CGTools.timer(ref CustomPropertiesCooldown[3], 1f))
                        {
                            photonPlayer3.SetCustomProperties(new Hashtable
                            {
                                {
                                    PhotonPlayerProperty.kills,
                                    redPoints
                                }
                            });
                            return;
                        }
                        CGTools.log("Too many Properties Changes setting Magenta ScoreKeeper kills");
                        return;
                    }
                    else
                    {
                        k++;
                    }
                }
            }
        }

        public static void ResetBall()
        {
            Vector3 location = ((RCRegion)FengGameManagerMKII.RCRegions["BallSpawn"]).location;
            ball.transform.position = location;
            ball.transform.rotation = Quaternion.identity;
            ball.GetComponent<Rigidbody>().velocity = Vector3.zero;
            ball.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        }
        
        public static void SpawnBall()
        {
            PhotonNetwork.Destroy(ball);
            PhotonNetwork.Destroy(bombTrail);
            PhotonNetwork.Destroy(ballMapIndicatorTitan);
            Vector3 location = ((RCRegion)FengGameManagerMKII.RCRegions["BallSpawn"]).location;
            ball = PhotonNetwork.Instantiate("RCAsset/CannonBallObject", location, Quaternion.identity, 0);
            ball.GetComponent<CannonBall>().isBall = true;
            bombTrail = PhotonNetwork.Instantiate("RCAsset/BombMain", location, Quaternion.identity, 0);
            bombTrail.GetComponent<Bomb>().CGBomb = true;
            ballMapIndicatorTitan = PhotonNetwork.Instantiate("TITAN_VER3.1", new Vector3(location.x, titanHeight, location.z), Quaternion.identity, 0);
            ballMapIndicatorTitan.GetComponent<TITAN>().isStatic = true;
            
        }

        public static void GoalExplosion(Vector3 vector3)
        {
            PhotonNetwork.Instantiate("fx/colossal_steam", vector3, Quaternion.identity, 0);
            foreach (GameObject gameObject in GameObject.FindGameObjectsWithTag("Player"))
            {
                Vector3 vector4 = gameObject.transform.position + Vector3.up * 2f - vector3;
                float goalExplosionForce = GoalExplosionForce;
                if (IN_GAME_MAIN_CAMERA.gametype == GAMETYPE.MULTIPLAYER && PhotonNetwork.isMasterClient)
                {
                    object[] parameters = new object[]
                    {
                        vector4.normalized * goalExplosionForce + Vector3.up * 1f
                    };
                    gameObject.GetComponent<HERO>().photonView.RPC("blowAway", PhotonTargets.All, parameters);
                }
            }
        }

        public static void OnInstantiate(PhotonPlayer photonPlayer, string key, GameObject instantiated)
        {
            if (PhotonNetwork.isMasterClient && doGameMode)
            {
                if (key == "FX/flareBullet3" && CGTools.GetHero(photonPlayer).rigidbody.velocity.magnitude < 1f)
                {
                    HERO hero2 = CGTools.GetHero(photonPlayer);
                    Vector3 vector2 = new Vector3(0f, 20f, 0f);
                    if (RCextensions.returnIntFromObject(hero2.photonView.owner.customProperties[PhotonPlayerProperty.RCteam]) == 1)
                    {
                        int num = UnityEngine.Random.Range(1, 3);
                        vector2 = ((RCRegion)FengGameManagerMKII.RCRegions["CyanGas" + num.ToString()]).location;
                        hero2.photonView.RPC("moveToRPC", photonPlayer, new object[]
                        {
                            vector2.x,
                            vector2.y,
                            vector2.z
                        });
                        return;
                    }
                    if (RCextensions.returnIntFromObject(photonPlayer.customProperties[PhotonPlayerProperty.RCteam]) == 2)
                    {
                        int num2 = UnityEngine.Random.Range(1, 3);
                        vector2 = (vector2 = ((RCRegion)FengGameManagerMKII.RCRegions["MagentaGas" + num2.ToString()]).location);
                        hero2.photonView.RPC("moveToRPC", photonPlayer, new object[]
                        {
                            vector2.x,
                            vector2.y,
                            vector2.z
                        });
                    }
                    
                }
                if (key == "hook" && disallowHooks)
                {

                    CGTools.GetHero(photonPlayer).photonView.RPC("netDie2", photonPlayer, new object[]
                    {
                        -1,
                        "Hooks are currently disabled"
                    });

                }
            }
        }

        public static void SetBallRigidbodySettings()
        {
            PhysicMaterial physicMaterial = new PhysicMaterial();
            physicMaterial.bounciness = PhysicsSettings[0];
            physicMaterial.dynamicFriction = PhysicsSettings[1];
            physicMaterial.dynamicFriction2 = PhysicsSettings[2];
            physicMaterial.staticFriction = PhysicsSettings[3];
            physicMaterial.staticFriction2 = PhysicsSettings[4];
            ball.GetComponent<Collider>().material = physicMaterial;
        }

        public static void OnPlayerJoined(PhotonPlayer photonPlayer)
        {
            
            if (doJoinCard)
            {
                FengGameManagerMKII.instance.photonView.RPC("Chat", photonPlayer, new object[]
                {
                    CGSoccer.joinMessage.hexColor(),
                    ""
                });
            }
        }

        public static void FindLastPlayerToHit(GameObject player)
        {
            if (player.GetPhotonView().owner != null)
            {
                PhotonPlayer owner = player.GetPhotonView().owner;
                if ((int)owner.customProperties[PhotonPlayerProperty.RCteam] == 1)
                {
                    LastCyanToHitBall = owner;
                    return;
                }
                if ((int)owner.customProperties[PhotonPlayerProperty.RCteam] == 2)
                {
                    LastMagentaToHitBall = owner;
                }
            }
        }

        public static void givePoint(bool TFRedBlue)
        {
            if (TFRedBlue)
            {
                if (LastMagentaToHitBall != null)
                {
                    int num = (int)LastMagentaToHitBall.customProperties[PhotonPlayerProperty.kills] + 1;
                    LastMagentaToHitBall.SetCustomProperties(new Hashtable
                    {
                        {
                            PhotonPlayerProperty.kills,
                            num
                        }
                    });
                    return;
                }
            }
            else if (LastCyanToHitBall != null)
            {
                int num2 = (int)LastCyanToHitBall.customProperties[PhotonPlayerProperty.kills] + 1;
                LastCyanToHitBall.SetCustomProperties(new Hashtable
                {
                    {
                        PhotonPlayerProperty.kills,
                        num2
                    }
                });
                return;
            }
        }

        public static void AnnouncePlayerScored(bool TFRedBlue)
        {
            if (anounceScore && restartEachRound)
            {
                string[] array = new string[]
                {
                    " "
                };
                FengGameManagerMKII.instance.photonView.RPC("clearlevel", PhotonTargets.AllBuffered, new object[]
                {
                    array,
                    3
                });
                string text;
                if (TFRedBlue)
                {
                    text = LastMagentaToHitBall.customProperties[PhotonPlayerProperty.name].ToString();
                }
                else
                {
                    text = LastCyanToHitBall.customProperties[PhotonPlayerProperty.name].ToString();
                }
                string text2 = string.Concat(new object[]
                {
                    "[00FFFF]Cyan: ",
                    bluePoints,
                    "          [FF00FF]Magenta: ",
                    redPoints,
                    Environment.NewLine,
                    Environment.NewLine,
                    text,
                    " [FFFFFF]Scored!"
                });
                if (FengGameManagerMKII.instance.roundTime < 20f)
                {
                    FengGameManagerMKII.instance.photonView.RPC("refreshStatus", PhotonTargets.AllBuffered, new object[]
                    {
                        0,
                        0,
                        0,
                        0,
                        200f,
                        20f,
                        true,
                        false
                    });
                }
                FengGameManagerMKII.instance.gameWin2();
                FengGameManagerMKII.instance.photonView.RPC("netRefreshRacingResult", PhotonTargets.All, new object[]
                {
                    text2
                });
            }
        }

        public static void OnRestart()
        {
            if (!doGameMode)
            {
                return;
            }
            if (anounceScore)
            {
                string[] array = new string[]
                {
                    " "
                };
                FengGameManagerMKII.instance.photonView.RPC("clearlevel", PhotonTargets.AllBuffered, new object[]
                {
                    array,
                    4
                });
            }
            needsNewRound = true;
            roundTimer = 1f;
        }

        public static void setTeam(PhotonPlayer player, int team)
        {
            FengGameManagerMKII.instance.photonView.RPC("setTeamRPC", player, new object[]
            {
                team
            });
            HERO hero = CGTools.GetHero(player);
            hero.markDie();
            hero.photonView.RPC("netDie2", PhotonTargets.All, new object[]
            {
                -1,
                "Team Switch"
            });
            FengGameManagerMKII.instance.playerKillInfoUpdate(PhotonNetwork.player, 0);
        }

        public static bool disallowHooks = false;

        public static string joinMessage = "[FF8000]AOT Custom Games Mod" + Environment.NewLine + "[FFFFFF]Created By Avisite";

        public static GameObject ball;

        public static int redPoints;

        public static int bluePoints;

        public static GameObject bombTrail;

        public static bool doGameMode;

        public static float bombIndicatorHeight;

        public static PhotonPlayer scoreKeeperMagenta;

        public static PhotonPlayer scoreKeeperCyan;

        public static float ScoreResetTimer;

        public static bool needsNewRound;

        public static float roundTimer;

        public static float ballRadius;

        public static float[] CustomPropertiesCooldown;

        public static GameObject ballMapIndicatorTitan;

        public static float titanHeight;

        public static float GoalExplosionForce;

        public static float BallForceMultiplier;

        public static float BallForceMaxSpeed;

        public static float[] PhysicsSettings;

        public static bool doJoinCard;

        public static PhotonPlayer LastCyanToHitBall;

        public static PhotonPlayer LastMagentaToHitBall;

        public static bool restartEachRound;

        public static bool anounceScore;

        public static float timeUntilRestart;

        public static bool needsRestart;

        public static bool useScoreKeepers;

        public static float ballSize = 5f;

        public static List<GameObject> allRockBalls = new List<GameObject>();
    }

    public class CGTools
    {
        public static bool timer(ref float timer, float waitTime)
        {
            if (timer <= Time.time)
            {
                timer = Time.time + waitTime;
                return true;
            }
            return false;
        }

        public static bool IsPointInRegion(Vector3 point, RCRegion region)
        {
            float num = region.dimX / 2f;
            float num2 = region.dimY / 2f;
            float num3 = region.dimZ / 2f;
            return point.x < region.location.x + num && point.x > region.location.x - num && point.y < region.location.y + num2 && point.y > region.location.y - num2 && point.z < region.location.z + num3 && point.z > region.location.z - num3;
        }

        public static void log(object o)
        {
            CGLog.log(o.ToString());
        }

        public static HERO GetHero(PhotonPlayer player)
        {
            foreach (GameObject H in GameObject.FindGameObjectsWithTag("Player"))
            {
                if (H.GetPhotonView().owner == player)
                {
                    return H.GetComponent<HERO>();
                }
            }
            return null;
        }

        public static TITAN GetTitan(PhotonPlayer player)
        {
            return null;
        }

        public static PhotonPlayer FindPlayer(string name) {
            foreach (PhotonPlayer p in PhotonNetwork.playerList)
            {
                if (p.name == name)
                {
                    return p;
                }
            }
            return null;
        }
    }

    public static class FlatUI
    {
        public static void Box(Rect rect)
        {
            GUI.DrawTexture(rect, outlineTexture);
            GUI.DrawTexture(new Rect(rect.x + (float)outlineThickness, rect.y + (float)outlineThickness, rect.width - (float)outlineThickness * 2f, rect.height - (float)outlineThickness * 2f), baseTexture);
        }

        public static Texture2D FlatColorTexture(Color color)
        {
            Texture2D texture2D = new Texture2D(1, 1);
            texture2D.SetPixels(new Color[]
            {
                color
            });
            texture2D.Apply();
            return texture2D;
        }

        public static void Start()
        {
            baseTexture = FlatColorTexture(new Color(0.2f, 0.2f, 0.2f));
            outlineTexture = FlatColorTexture(Color.black);
            Oarnge = FlatColorTexture(new Color(1f, 0.5f, 0f));
        }

        public static Rect split(Rect input, int index, int columns)
        {
            float num = input.width / (float)columns;
            return new Rect(input.x + (float)index * num, input.y, num, input.height);
        }

        public static Rect split(Rect input, int index, int columns, int width)
        {
            float num = input.width / (float)columns;
            return new Rect(input.x + (float)index * num, input.y, num * (float)width, input.height);
        }

        public static void Box(Rect rect, Texture2D insideTexture)
        {
            GUI.DrawTexture(rect, outlineTexture);
            GUI.DrawTexture(new Rect(rect.x + (float)outlineThickness, rect.y + (float)outlineThickness, rect.width - (float)outlineThickness * 2f, rect.height - (float)outlineThickness * 2f), insideTexture);
        }

        public static Texture2D baseTexture;

        public static Texture2D outlineTexture;

        public static Texture2D darkTexture;

        public static Texture2D lightTexture;

        public static int outlineThickness = 2;

        public static Texture2D Oarnge;
    }

    public class KaneGameManager
    {
        public static void setBackground()
        {
            if (File.Exists("CGAssets/background.png"))
            {
                SpriteRenderer component = GameObject.Find("backgroundTex1").GetComponent<SpriteRenderer>();
                byte[] data = File.ReadAllBytes("CGAssets/background.png");
                Texture2D texture2D = new Texture2D(2, 2);
                texture2D.LoadImage(data);
                float num = (float)Screen.width / 2f;
                float num2 = (float)Screen.height / 2f;
                UnityEngine.Sprite sprite = UnityEngine.Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, 0.5f), 2f, 0U, SpriteMeshType.Tight);
                component.sprite = sprite;
                component.transform.position = new Vector3(0f, 0f, -15.3f);
            }
        }

        public static void Start()
        {
            FlatUI.Start();
            CGLog.start();
            CGSettingsMenu.Start();
            CGPrefs.LoadConfigFile();
        }

        public static void Update()
        {
            CGCustomLevelManager.Update();
            CGSoccer.update();
            CGLevelEditor.Update();
        }

        public static void FixedUpdate()
        {
        }

        public static void OnRestart()
        {
            CGCustomLevelManager.OnRestart();
            CGSoccer.OnRestart();
        }

        public static void OnInstantiate(PhotonPlayer photonPlayer, string key, GameObject Instantiated)
        {
            CGSoccer.OnInstantiate(photonPlayer, key, Instantiated);
        }

        public static void OnChat(PhotonPlayer sender, string message)
        {
        }

        public static void OnGUI()
        {
            CGLog.onGUI();
        }

        public static void OnPhotonPlayerConnected(PhotonPlayer photonPlayer)
        {
            CGCustomLevelManager.OnPhotonPlayerConnected(photonPlayer);
            CGSoccer.OnPlayerJoined(photonPlayer);
        }
    }

    public struct logMessage
    {
        public logMessage(string _message, Color _color, int _severity)
        {
            this.message = _message;
            this.color = _color;
            this.severity = _severity;
        }

        public logMessage(string _message, Color _color)
        {
            this.message = _message;
            this.color = _color;
            this.severity = 0;
        }

        public logMessage(string _message, int _severity)
        {
            this.message = _message;
            this.color = Color.white;
            this.severity = _severity;
        }

        public logMessage(string _message)
        {
            this.message = _message;
            this.color = Color.white;
            this.severity = 0;
        }

        public string message;

        public Color color;

        public int severity;
    }
}
