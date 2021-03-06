﻿//#define USE_TOUCH_SCRIPT

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * ボタンオブジェクトのベースクラス
 */
namespace GarageKit
{
    // Press&Releaseの連携処理コンポーネント格納用
    [Serializable]
    public class RelationalComponentData
    {
        public MonoBehaviour component = null;
        public string pressFunctionName = "";
        public string releaseFunctionName = "";
    }

    [RequireComponent(typeof(BoxCollider))]
    public class ButtonObjectBase : MonoBehaviour
    {
        // ボタンの実行タイプ
        public enum BUTTON_TYPE
        {
            CLICK = 0,
            PRESS,
            RELEASE,
            PRESSHOLD
        }

        // 入力タイプ
        public enum INPUT_TYPE
        {
            WTOUCH = 0,
            INPUTTOUCH,
            MOUSE
        }

        // 共通入力フェーズ
        public enum INPUT_PHASE
        {
            Began = 0,
            Canceled,
            Ended,
            Moved,
            Stationary,
            NONE
        }

        // ボタンの有効確認
        private bool isEnableButton = true;
        public bool IsEnableButton { get{ return isEnableButton; } }

        // 入力タイプ
        public static INPUT_TYPE inputType = INPUT_TYPE.MOUSE;

        // ボタンの実行タイプ
        public BUTTON_TYPE buttonType = BUTTON_TYPE.CLICK;

        // Hitチェックの重なりを考慮
        public bool asFirstResponder = true;

        // ボタンテクスチャの切り替え
        public GameObject guiPlate;
        private Color defaultButtonColor;
        public Color disableButtonColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        public Texture2D offBtnTexture;
        public Texture2D onBtnTexture;

        // トグル処理用
        public bool isToggleButton = false;
        private bool toggleState = false;
        public bool ToggleState { get{ return toggleState; } }

        // Press&Releaseの連携処理コンポーネント
        public RelationalComponentData[] relationalComponents;

        private Camera rayCamera;
        private bool inputEnable = true; //入力有効判定
        public bool InputEnable { get{ return inputEnable; } }
        private bool isTouch = true; //タッチしてるかどうか
        private bool isPressed = false; //Pressタイプのとき、Pressできたかどうか
        private int touchCount;
        private Vector3 touchPosition; 
        public Vector3 TouchPosition { get{ return touchPosition; } }
        private INPUT_PHASE phase = INPUT_PHASE.NONE;

        // Pressされたボタンのカウント
        static public int PressBtnsTotal = 0;


        protected virtual void Awake()
        {

        }

        protected virtual void Start()
        {
            // 設定ファイルから入力モードの取得
            if(ApplicationSetting.Instance.GetString("InputMode").ToLower() == "mouse")
                inputType = INPUT_TYPE.MOUSE;
            else
                inputType = INPUT_TYPE.WTOUCH;

            // 自分を描画するカメラを取得
            rayCamera = CameraUtil.FindCameraForLayer(this.gameObject.layer);

            // ボタンテクスチャの設定
            if(guiPlate != null && guiPlate.GetComponent<Renderer>() != null)
                defaultButtonColor = guiPlate.GetComponent<Renderer>().material.color;
            ChangeTexture(false);
        }

        protected virtual void Update()
        {
            // デバイスチェック
            InputDevice();

            // ボタン入力チェック
            CheckTouchButton();

            // トグルボタン時にテクスチャを上書き設定
            if(isToggleButton && !isPressed)
                ChangeTexture(toggleState);
        }


        /// <summary>
        /// 入力デバイスにより分岐する
        /// </summary>
        private void InputDevice()
        {
            isTouch = true;
            touchCount=0;

            // for WinTouch
            if(inputType == INPUT_TYPE.WTOUCH)
            {				
#if UNITY_STANDALONE_WIN

#if !USE_TOUCH_SCRIPT
                touchCount = Input.touchCount;

                // 1点目を入力として受付
                if(touchCount >= 1)
                {
                    Rect wrect = WindowsUtil.GetApplicationWindowRect(); // not work in Editor.
                    touchPosition = new Vector2(
                        (int)(((Input.touches[0].position.x / Screen.width) * Screen.currentResolution.width) - wrect.x),
                        Screen.height + (int)(((Input.touches[0].position.x / Screen.height) * Screen.currentResolution.height) - wrect.y));
                    
                    if(Input.touches[0].deltaPosition != Vector2.zero)
                    {
                        if(isPressed)
                            phase = INPUT_PHASE.Moved;
                        else
                            phase = INPUT_PHASE.Began;
                    }
                    else
                        phase = INPUT_PHASE.Moved;
#else
                touchCount = TouchScript.TouchManager.Instance.PressedPointersCount;

                // 1点目を入力として受付
                if(touchCount >= 1)
                {
                    TouchScript.Pointers.Pointer tp = TouchScript.TouchManager.Instance.PressedPointers[0];
                    Rect wrect = WindowsUtil.GetApplicationWindowRect(); // not work in Editor.
                    touchPosition = new Vector2(
                        (int)(((tp.Position.x / Screen.width) * Screen.currentResolution.width) - wrect.x),
                        Screen.height + (int)(((tp.Position.y / Screen.height) * Screen.currentResolution.height) - wrect.y));
                    
                    if(tp.Position == tp.PreviousPosition)
                    {
                        if(isPressed)
                            phase = INPUT_PHASE.Moved;
                        else
                            phase = INPUT_PHASE.Began;
                    }
                    else
                        phase = INPUT_PHASE.Moved;
#endif
                }
                else
                {
                    // Pressされた後にここにきたらReleaseとする
                    if(isPressed)
                    {
                        phase = INPUT_PHASE.Ended;
                    }
                    else
                    {
                        phase = INPUT_PHASE.NONE;
                        touchPosition = Vector3.zero;
                        isTouch = false;
                    }
                }
#endif		
            }

            // for Mobile
            else if(inputType == INPUT_TYPE.INPUTTOUCH)
            {
                touchCount = Input.touchCount;
                
                if(touchCount >= 1)
                {
                    touchPosition = Input.GetTouch(0).position;	
                    TouchPhase tphase = Input.GetTouch(0).phase;
                    
                    if(tphase == TouchPhase.Began)
                        phase = INPUT_PHASE.Began;
                    else if(tphase == TouchPhase.Ended || tphase == TouchPhase.Canceled)
                        phase = INPUT_PHASE.Ended;
                    else if(tphase == TouchPhase.Moved || tphase == TouchPhase.Stationary)
                        phase = INPUT_PHASE.Moved;
                }
                else
                {
                    phase = INPUT_PHASE.NONE;
                    touchPosition = Vector3.zero;
                    isTouch = false;
                }
            }

            // for Mouse
            else if(inputType == INPUT_TYPE.MOUSE)
            {
                touchPosition = Input.mousePosition;
                
                if(Input.GetMouseButton(0))
                {
                    if(Input.GetMouseButtonDown(0))
                        phase = INPUT_PHASE.Began;
                    else
                        phase = INPUT_PHASE.Moved;
                    
                    touchCount = 1;
                }
                else if(Input.GetMouseButtonUp(0))
                    phase = INPUT_PHASE.Ended;
                else
                    phase = INPUT_PHASE.NONE;
            }

            if(touchCount < 0)
                touchCount = 0;
        }

        /// <summary>
        /// ボタンが押されているかチェックする
        /// </summary>
        private void CheckTouchButton()
        {
            if(!isTouch || !inputEnable)
            {
                Unhit();
                return;
            }

            // タッチ判定を行う
            RaycastHit hit = new RaycastHit();
            Ray ray = rayCamera.ScreenPointToRay(touchPosition);
            if(asFirstResponder)
            {
                // 重なりを考慮してチェック
                if(Physics.Raycast(ray, out hit, Mathf.Infinity))
                {
                    if(hit.collider.gameObject == this.gameObject)
                        Hit();
                    else
                        Unhit();
                }
                else
                    Unhit();
            }
            else
            {
                // 重なりは無視してチェック
                if(this.GetComponent<Collider>().Raycast(ray, out hit, Mathf.Infinity))
                    Hit();
                else
                    Unhit();
            }
        }

        /// <summary>
        /// RaycastしてHitした場合の処理
        /// <summary>
        private void Hit()
        {
            if(phase == INPUT_PHASE.Began) // Pressした瞬間
            {
                isPressed = true;
                
                OnPressButton(this.gameObject, true);
            }
            else if(phase == INPUT_PHASE.Moved) // 押している間
            {
                if(isPressed)
                    OnPressHoldButton(this.gameObject);
            }
            else if(phase == INPUT_PHASE.Ended) // 離した瞬間
            {
                if(isPressed)
                {
                    OnPressButton(this.gameObject, false);	
                    OnClickButton(this.gameObject);
                }

                isPressed = false;
            }
            else if(phase == INPUT_PHASE.NONE) // オーバー
            {
                OnHoverButton(this.gameObject, true);
                
                isPressed = false;
            }
        }

        /// <summary>
        /// RaycastしてHitしなかった場合の処理
        /// <summary>
        private void Unhit()
        {
            if(buttonType == BUTTON_TYPE.PRESS ||
                buttonType == BUTTON_TYPE.PRESSHOLD ||
                buttonType == BUTTON_TYPE.CLICK)
            {
                // Began時にOnPressしていれば処理
                if(phase == INPUT_PHASE.Ended && isPressed)
                {
                    OnPressButton(this.gameObject, false);
                
                    isPressed = false;
                }
            }
            else
            {
                isPressed = false;
            }
        }

#region Button Function
        protected virtual void OnPressButton(GameObject sender, bool pressed)
        {
            if(pressed)
            {
                // Pressされたボタンのカウント
                PressBtnsTotal++;

                // Pressのコンポーネント連携
                RelationalComponentFunc(true);

                // テクスチャを切り替え
                ChangeTexture(true);

                // ボタン処理
                if(buttonType == BUTTON_TYPE.PRESS)
                    OnButton();
            }
            else
            {
                // Pressされたボタンのカウント
                PressBtnsTotal--;
                if(PressBtnsTotal < 0)
                    PressBtnsTotal = 0;

                // Releaseのコンポーネント連携
                RelationalComponentFunc(false);

                // テクスチャを切り替え
                if(!isToggleButton)
                    ChangeTexture(false);

                // ボタン処理
                if(buttonType == BUTTON_TYPE.RELEASE)
                    OnButton();
            }
        }

        protected virtual void OnPressHoldButton(GameObject sender)
        {
            // ボタン処理
            if(buttonType == BUTTON_TYPE.PRESSHOLD)
                OnButton();
        }

        protected virtual void OnDragButton(GameObject sender, Vector2 dragDelta) { }

        protected virtual void OnHoverButton(GameObject sender, bool overed) { }

        protected virtual void OnClickButton(GameObject sender)
        {	
            // ボタン処理
            if(buttonType == BUTTON_TYPE.CLICK)
                OnButton();
        }


        /// <summary>
        /// ボタン処理
        /// </summary>
        protected virtual void OnButton()
        {
            // トグルボタン
            if(isToggleButton)
            {
                toggleState = !toggleState;

                OnToggleButton(toggleState);
            }
        }

        /// <summary>
        /// トグルボタン処理
        /// </summary>
        protected virtual void OnToggleButton(bool toggleState) {}
#endregion

        /// <summary>
        /// Press/Releaseのコンポーネント連携
        /// </summary>
        private void RelationalComponentFunc(bool state)
        {
            foreach(RelationalComponentData componentData in relationalComponents)
            {
                if(componentData != null)
                {
                    if(state)
                        componentData.component.SendMessage(componentData.pressFunctionName, SendMessageOptions.DontRequireReceiver);
                    else
                        componentData.component.SendMessage(componentData.releaseFunctionName, SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        /// <summary>
        /// //テクスチャを切り替え
        /// </summary>
        private void ChangeTexture(bool pressed)
        {
            if(pressed)
            {
                if(guiPlate != null && guiPlate.GetComponent<Renderer>() != null && onBtnTexture != null)
                    guiPlate.GetComponent<Renderer>().material.mainTexture = onBtnTexture;
            }
            else
            {
                if(guiPlate != null && guiPlate.GetComponent<Renderer>() != null && offBtnTexture != null)
                    guiPlate.GetComponent<Renderer>().material.mainTexture = offBtnTexture;
            }
        }

        /// <summary>
        /// ボタンの有効化
        /// </summary>
        public void EnableButton()
        {
            isEnableButton = true;

            // マテリアル変更
            if(guiPlate != null && guiPlate.GetComponent<Renderer>() != null)
                guiPlate.GetComponent<Renderer>().material.color = defaultButtonColor;

            // コライダをON
            if(this.gameObject.GetComponent<Collider>() != null)
                this.gameObject.GetComponent<Collider>().enabled = true;
        }

        /// <summary>
        /// ボタンの無効化
        /// </summary>
        public void DisableButton()
        {
            isEnableButton = false;

            // マテリアル変更
            if(guiPlate != null && guiPlate.GetComponent<Renderer>() != null)
                guiPlate.GetComponent<Renderer>().material.color = disableButtonColor;

            // コライダをOFF
            if(this.gameObject.GetComponent<Collider>() != null)
                this.gameObject.GetComponent<Collider>().enabled = false;
        }

        /// <summary>
        /// ボタンのリセット
        /// </summary>
        public void ResetButton()
        {
            toggleState = false;
            ChangeTexture(false);
        }

        /// <summary>
        /// 入力の有効化
        /// </summary>
        public void EnableInput()
        {
            inputEnable = true;
        }

        /// <summary>
        /// 入力の無効化
        /// </summary>
        public void DisableInput()
        {
            inputEnable = false;
        }
    }
}
