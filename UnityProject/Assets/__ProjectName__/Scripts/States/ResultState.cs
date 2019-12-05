﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using GarageKit;

public class ResultState : AsyncStateBase, ISequentialState
{
	public Text sceneText;
	public Text timerText;
	public Text messageText;
	
	
	public override void StateStart(object context)
	{
		base.StateStart(context);
		
		sceneText.text = "this is [Result] state.";
		timerText.text = "timer is end";
		messageText.text = "push [Space] return to [Wait] state.";
	}
	
	public override void StateUpdate()
	{
		base.StateUpdate();
	}
	
	public override void StateExit()
	{
		base.StateExit();
	}


	public void ToNextState()
	{
		AppMain.Instance.sceneStateManager.ChangeAsyncState("WAIT");
	}

	public void ToPrevState()
	{
		AppMain.Instance.sceneStateManager.ChangeAsyncState("PLAY");
	}
}
