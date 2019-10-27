// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;
using System.Reflection;


namespace Microsoft.Azure.SpatialAnchors.Unity.Examples
{
    public class ARSphere: ARbase
    {
        internal enum AppState
        {
            SelectStep,
            LookForAnchor,
            StepComplete,
            CreateSession,
            ConfigSession,
            LookingForAnchor,
            CreateLocalAnchor,
            StartSession,
            Complete,
            SaveCloudAnchor,
            SavingCloudAnchor,
            StopSession

        }

        internal enum AppMode
        {
            finding,
            saving,
            none
        }

        private readonly Dictionary<AppState, DemoStepParams> stateParams = new Dictionary<AppState, DemoStepParams>
        {

           
            { AppState.SelectStep, new DemoStepParams() { StepMessage = "Select Function", StepColor = Color.clear }},
            { AppState.CreateSession,new DemoStepParams() { StepMessage = "Next: Create Azure Spatial Anchors Session", StepColor = Color.clear }},
            { AppState.ConfigSession,new DemoStepParams() { StepMessage = "Next: Configure Azure Spatial Anchors Session", StepColor = Color.clear }},
            { AppState.LookForAnchor, new DemoStepParams() { StepMessage = "Looking For Anchor", StepColor = Color.clear }},
            { AppState.Complete, new DemoStepParams() { StepMessage = "Completed", StepColor = Color.clear }},
            { AppState.LookingForAnchor, new DemoStepParams() { StepMessage = "Looking For Anchor", StepColor = Color.clear }},
            { AppState.CreateLocalAnchor, new DemoStepParams() { StepMessage = "Placing local Anchor", StepColor = Color.clear }},
            { AppState.StartSession, new DemoStepParams() { StepMessage = "Starting Session", StepColor = Color.clear }},
            { AppState.SaveCloudAnchor, new DemoStepParams() { StepMessage = "Saving Cloud Anchor", StepColor = Color.clear }},
            { AppState.SavingCloudAnchor, new DemoStepParams() { StepMessage = "Saving to cloud", StepColor = Color.clear }},
            { AppState.StopSession, new DemoStepParams() { StepMessage = "Stop Session", StepColor = Color.clear }}


        };

        private AppState _currentAppState = AppState.SelectStep;
        private AppMode _currentMode = AppMode.none;

        AppState currentAppState
        {
            get
            {
                return _currentAppState;
            }
            set
            {
                if (_currentAppState != value)
                {
                    Debug.LogFormat("State from {0} to {1}", _currentAppState, value);
                    _currentAppState = value;
                    if (spawnedObjectMat != null)
                    {
                        spawnedObjectMat.color = stateParams[_currentAppState].StepColor;
                    }

                    if (!isErrorActive)
                    {
                        feedbackBox.text = stateParams[_currentAppState].StepMessage;
                    }
                }
            }
        }

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

            feedbackBox.text = stateParams[currentAppState].StepMessage;

            Debug.Log("Azure Spatial Anchors Demo script started");
        }




       



        protected override void OnCloudAnchorLocated(AnchorLocatedEventArgs args)
        {
            base.OnCloudAnchorLocated(args);

            if (args.Status == LocateAnchorStatus.Located)
            {
                currentCloudAnchor = args.Anchor;

                UnityDispatcher.InvokeOnAppThread(() =>
                {
                    currentAppState = AppState.StopSession;
                    Pose anchorPose = Pose.identity;

                    feedbackBox.text = "Anchor Found!";

#if UNITY_ANDROID || UNITY_IOS
                    anchorPose = currentCloudAnchor.GetPose();
#endif
                    // HoloLens: The position will be set based on the unityARUserAnchor that was located.
                    SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);
                });
            }
        }

        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        public override void Update()
        {
            base.Update();

            runGps();

            if (spawnedObjectMat != null)
            {
                float rat = 0.1f;
                float createProgress = 0f;
                if (CloudManager.SessionStatus != null)
                {
                    createProgress = CloudManager.SessionStatus.RecommendedForCreateProgress;
                }
                rat += (Mathf.Min(createProgress, 1) * 0.9f);
                spawnedObjectMat.color = GetStepColor() * rat;
            }


            
        }

        protected override bool IsPlacingObject()
        {
            return currentAppState == AppState.CreateLocalAnchor;
        }

        protected override Color GetStepColor()
        {
            return stateParams[currentAppState].StepColor;
        }

   
      
        protected void saveAnchorId(string id)
        {
            double x = 0;
            double y = 0;

            double.TryParse(LongitudeBox.text, out x);
            double.TryParse(LatitudeBox.text, out y);




            connection.Invoke<Task>("CreateAnchor", new
            {
                ID = id,
                X = x,
                Y = y,
                Model = 0,
                Creator = 0

            }).OnSuccess((ret) =>
            {
                Debug.Log("saved the cloud anchor");

            }).OnError((err) => {

                Debug.LogError(err);

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

            currentAppState = AppState.StopSession;
        }

        protected override void OnSaveCloudAnchorFailed(Exception exception)
        {
            base.OnSaveCloudAnchorFailed(exception);

            currentAnchorId = string.Empty;
        }

        public async override Task AdvanceDemoAsync()
        {
            switch (currentAppState)
            {
                case AppState.SelectStep:
                    if (_currentMode != AppMode.none) currentAppState = AppState.CreateSession;
                    break;

                case AppState.CreateSession:
                    if (CloudManager.Session == null)
                    {
                        await CloudManager.CreateSessionAsync();
                    }
                    currentAnchorId = "";
                    currentCloudAnchor = null;
                    currentAppState = AppState.ConfigSession;
                    break;

                case AppState.ConfigSession:
                    ConfigureSession();
                    currentAppState = AppState.StartSession;
                    break;

                case AppState.StartSession:

                    await CloudManager.StartSessionAsync();

                    if(_currentMode == AppMode.finding) currentAppState = AppState.LookForAnchor;
                    if(_currentMode == AppMode.saving) currentAppState = AppState.CreateLocalAnchor;

                    break;

                case AppState.LookForAnchor:
                    currentAppState = AppState.LookingForAnchor;
                    currentWatcher = CreateWatcher();
                    break;

                case AppState.LookingForAnchor:
                    break;

                case AppState.CreateLocalAnchor:
                    if (spawnedObject != null)
                    {
                        currentAppState = AppState.SaveCloudAnchor;
                    }
                    break;

                case AppState.SaveCloudAnchor:
                    currentAppState = AppState.SavingCloudAnchor;
                    await SaveCurrentObjectAnchorToCloudAsync();
                    break;

                case AppState.StopSession:
                    CloudManager.StopSession();
                    currentWatcher = null;
                    currentAppState = AppState.Complete;
                    break;

                case AppState.Complete:
                    currentCloudAnchor = null;
                    currentAppState = AppState.SelectStep;
                    break;

                default:
                    Debug.Log("Shouldn't get here for app state " + currentAppState.ToString());
                    break;
            }
        }


     

        public void findAnchors()
        {
            if (currentAppState == AppState.SelectStep)
            {
                _currentMode = AppMode.finding;
            }

        }

        public void saveAnchors()
        {
            if (currentAppState == AppState.SelectStep)
            {
                _currentMode = AppMode.saving;
            }
        }

        public void ConfigureSession()
        {
            List<string> anchorsToFind = new List<string>();

           if (_currentMode == AppMode.finding)
            {



                connection.Invoke<dynamic>("GetLastAnchor")
                .OnSuccess((ret) =>
                {

                    string idToFind = ret["id"];

                    Debug.Log(idToFind);

                    anchorId.text = idToFind;


                    anchorsToFind.Add(idToFind);

                    SetAnchorIdsToLocate(anchorsToFind);
                    

                });



            }
            else
            {
                SetAnchorIdsToLocate(anchorsToFind);
            }

            
        }
    }
}
