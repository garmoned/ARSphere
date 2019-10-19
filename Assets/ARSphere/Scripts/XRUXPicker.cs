// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.Azure.SpatialAnchors.Unity.Examples
{
    /// <summary>
    /// Picks the appropriate UI game object to be used. 
    /// This allows us to have both HoloLens and Mobile UX in the same
    /// scene.
    /// </summary>
    public class XRUXPicker : MonoBehaviour
    {
        private static XRUXPicker _Instance;
        public static XRUXPicker Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = FindObjectOfType<XRUXPicker>();
                }

                return _Instance;
            }
        }

        public GameObject HoloLensUXTree;
        public GameObject MobileAndEditorUXTree;

        void Awake()
        {

            MobileAndEditorUXTree.SetActive(true);

        }

        /// <summary>
        /// Gets the correct feedback text control for the demo
        /// </summary>
        /// <returns>The feedback text control if it found it</returns>
        /// 



        public Text getAnchorToFind()
        {

            


            if(GameObject.FindGameObjectWithTag("AnchorToFind").GetComponent<Text>() != null)
            {
                return GameObject.FindGameObjectWithTag("AnchorToFind").GetComponent<Text>();
            }

            
            return null;

            
        }

        public Text getAnchorId()
        {

           

            if (GameObject.FindGameObjectWithTag("AnchorID").GetComponent<Text>() != null)
            {
               
    
                return GameObject.FindGameObjectWithTag("AnchorID").GetComponent<Text>();
                
            }

            


            return null;
        }


        public Text GetFeedbackText()
        {
            return GameObject.FindGameObjectWithTag("FeedBackText").GetComponent<Text>();
        }

        /// <summary>
        /// Gets the button used in the demo.
        /// </summary>
        /// <returns>The button used in the demo.  Returns null on HoloLens</returns>
        /// 
        public Text getLatitudeBox()
        {
            Text t = GameObject.FindGameObjectWithTag("LatitudeBox").GetComponent<Text>();

            if(t == null) Debug.Log("did not find latitude box");
                
            return t ;
        }

        public Text getLongitudeBox()
        {
            Text t = GameObject.FindGameObjectWithTag("LongitudeBox").GetComponent<Text>();

            if (t == null) Debug.Log("did not find longitude box");

            return t;
        }


        public Button GetDemoButton()
        {

         

            return GameObject.FindGameObjectWithTag("NextStep").GetComponent<Button>();

        }
    }
}
