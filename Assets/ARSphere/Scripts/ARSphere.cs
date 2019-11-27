
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Microsoft.Azure.SpatialAnchors.Unity.Examples
{
    public class ARSphere : ARbase
    {

        internal enum AppMode
        {
            startSaving,
            initSession,
            makingSession,
            saving,
            gettingNewData,
            placing,
            none
        }




        private AppMode _currentMode = AppMode.initSession;

        private readonly List<GameObject> allSpawnedObjects = new List<GameObject>();
        private readonly List<Material> allSpawnedMaterials = new List<Material>();


        private string currentAnchorId = "";

        /// <summary>
        /// Start is called on the frame when a script is enabled just before any
        /// of the Update methods are called the first time.
        /// </summary>
        public override void Start()
        {
            Debug.Log(">>Azure Spatial Anchors Demo Script Start");
  
            base.Start();


            if (!SanityCheckAccessConfiguration())
            {
                return;
            }


            Debug.Log("Azure Spatial Anchors Demo script started");
        }

        protected override void OnCloudAnchorLocated(AnchorLocatedEventArgs args)
        {
            base.OnCloudAnchorLocated(args);

            if (args.Status == LocateAnchorStatus.Located)
            {
                

                UnityDispatcher.InvokeOnAppThread(() =>
                {


                    foundId = args.Anchor.Identifier;

                    currentCloudAnchor = args.Anchor;

                    Pose anchorPose = Pose.identity;


                    anchorPose = currentCloudAnchor.GetPose();


                    spawnedObject = null;
                    currentCloudAnchor = null;



                    feedbackBox.text = "Anchor Found!";
                        
    
                    SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);

                });
            }
        }

        protected override void SpawnOrMoveCurrentAnchoredObject(Vector3 worldPos, Quaternion worldRot)
        {
         

            bool spawnedNewObject = spawnedObject == null;

            base.SpawnOrMoveCurrentAnchoredObject(worldPos, worldRot);

            if (spawnedNewObject)
            {
                allSpawnedObjects.Add(spawnedObject);
                allSpawnedMaterials.Add(spawnedObjectMat);
            }


        }



        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        /// 



        public async override void Update()
        {
            base.Update();

            runGps();


            Debug.Log(currentCloudAnchor);

            selectedModel = XRUXPicker.Instance.getSelectedModel();


            if(_currentMode  != AppMode.saving )feedbackBox.text = _currentMode.ToString();

            Debug.Log(needsNewData);
            Debug.Log(_currentMode);

            if (_currentMode == AppMode.initSession)
            {

                _currentMode = AppMode.makingSession;

                currentAnchorId = "";
                currentCloudAnchor = null;


                if (CloudManager.Session == null) {

                   await CloudManager.CreateSessionAsync();
                   await CloudManager.StartSessionAsync();
              }  
                
              
                _currentMode = AppMode.none;

            }


           if (needsNewData && _currentMode == AppMode.none && connection.State == BestHTTP.SignalRCore.ConnectionStates.Connected)
            {


                Debug.Log("getting New data");


                _currentMode = AppMode.gettingNewData;
                needsNewData = false;
                CloudManager.StopSession();
                connection.Invoke<dynamic[]>("GetNearbyAnchors", (double)Input.location.lastData.longitude, (double)Input.location.lastData.latitude)
                .OnSuccess( async (ret) =>
                {

                    Debug.Log("got data back!!");

                    List<string> anchorsToFind = new List<string>();
                    idToModelMap = new Dictionary<string, string>();

                    
                    foreach (dynamic var in ret)
                    {

                        if(var["model"] != null) {
                            Debug.Log(var["model"]["id"]);
                            Debug.Log(var["model"]["name"]);
                        }


                        idToModelMap.Add(var["id"], var["model"] == null ? "Default" : var["model"]["name"]);

                        anchorsToFind.Add(var["id"]);

                    }


                    SetAnchorIdsToLocate(anchorsToFind);

                    await CloudManager.ResetSessionAsync();

                    await CloudManager.StartSessionAsync()
                    .ContinueWith(state => {

                        SetGraphEnabled(true);
                        currentWatcher = CreateWatcher();
                        _currentMode = AppMode.none;

                    });               
                    

                   

                })
                .OnError(err =>
                {

                    Debug.Log(err);

                });
            }
            
            if(_currentMode == AppMode.startSaving)
            {
                

                Debug.Log("configuring for saving");

                spawnedObject = null;
                currentCloudAnchor = null;

                CloudManager.StopSession();

                ConfigureSession();

                await CloudManager.StartSessionAsync().ContinueWith(state => {

                    _currentMode = AppMode.placing;


                });


                



            }
             

            if (spawnedObjectMat != null)
            {
                float rat = 0.1f;
                float createProgress = 0f;
                if (CloudManager.SessionStatus != null)
                {
                    createProgress = CloudManager.SessionStatus.RecommendedForCreateProgress;
                }
                rat += (Mathf.Min(createProgress, 1) * 0.9f);
            }


        }


 

        protected override bool IsPlacingObject()
        {
            return _currentMode == AppMode.placing;
        }



        protected void saveAnchorId(string id)
        {


            int modelNumber;

            Debug.Log(id);


            switch (selectedModel)
            {
                case "Hot Dog":
                    modelNumber = 1;
                    break;
                case "Space Ship":
                    modelNumber = 2;
                    break;
                default:
                    modelNumber = 3;
                    break;
            }

            Debug.Log(modelNumber);

            var anchorModel = new
            {


                Id = id,
                Longitude = (double) Input.location.lastData.longitude,
                Latitude = (double) Input.location.lastData.latitude,
                Model = modelNumber


            };

            connection.Invoke<dynamic>("CreateAnchor", anchorModel).OnComplete(ret =>
            {
                Debug.Log("saved successsfully to signalR");
                _currentMode = AppMode.none;

            }).OnError(err=> {
                Debug.Log(err);
            });
        }   


        protected override async Task OnSaveCloudAnchorSuccessfulAsync()
        {
            await base.OnSaveCloudAnchorSuccessfulAsync();

            Debug.Log("Anchor created, yay!");

            currentAnchorId = currentCloudAnchor.Identifier;

            saveAnchorId(currentCloudAnchor.Identifier);

            // Sanity check that the object is still where we expect
            Pose anchorPose = Pose.identity;

#if UNITY_ANDROID || UNITY_IOS
            anchorPose = currentCloudAnchor.GetPose();
#endif
            // HoloLens: The position will be set based on the unityARUserAnchor that was located.

            SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);




        }

        protected override void OnSaveCloudAnchorFailed(Exception exception)
        {
            base.OnSaveCloudAnchorFailed(exception);

            currentAnchorId = string.Empty;
        }
     





            public void ConfigureSession()
        {
            List<string> anchorsToFind = new List<string>();

            SetAnchorIdsToLocate(anchorsToFind);
            

        }

        public async void Save()
        {
            if(_currentMode == AppMode.none)
            {
                _currentMode = AppMode.startSaving;
            }

            if(_currentMode == AppMode.placing && spawnedObject != null )
            {
                _currentMode = AppMode.saving;

                await SaveCurrentObjectAnchorToCloudAsync();
               
                
            }

        }
    }
}
