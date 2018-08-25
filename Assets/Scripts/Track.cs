﻿using UnityEngine;
using System.Collections;

public class Track : DrawableObject {
    public ProjectData.TrackData data;
    public float trackProgress;
    public static float TRACK_SCREEN_HEIGHT = 0.85f;
    public static float TRACK_SCREEN_WIDTH = 0.12f; // at size = 1.0f
    public int ID;
    public bool activeHover;
    public float currentWidth;
    public float flashEffectTime;
    public float pulseFlashEffectTime;
    public ConfirmBox deletionConfirm;

    public Track(ProjectData.TrackData data)
    {
        this.data = data;
        ID = data.id; 
    }

    public bool MouseOver
    {
        get {
            Vector3 mouse = Input.mousePosition;
            return mouse.x >= pos.x - Mathf.Abs(currentWidth) * 0.5f && mouse.x <= pos.x + Mathf.Abs(currentWidth) * 0.5f && mouse.y > VoezEditor.windowRes.y * (1f - TRACK_SCREEN_HEIGHT);
        }
    }

    public override void Update()
    {
        base.Update();
        trackProgress = (VoezEditor.Editor.songTime - data.start) / (data.end - data.start);
        pos.y = VoezEditor.windowRes.y * (1.0f - TRACK_SCREEN_HEIGHT);
        float desiredX = GetXAtTime(VoezEditor.Editor.songTime);
        pos.x = Util.ScreenPosX(desiredX);

        if (trackProgress > 1f || trackProgress < 0f)
            Destroy();
        if (flashEffectTime > 0)
            flashEffectTime -= 1;
        if (pulseFlashEffectTime > 0)
            pulseFlashEffectTime -= 1;

        // BPM Pulsing in BPM Edit Mode
        if (VoezEditor.Editor.ui.bpmButton.toggled && VoezEditor.Editor.musicPlayer.source.isPlaying) {
            float timeIncrement = 0;
            if (VoezEditor.Editor.project.songBPM > 0) {
                float secondsPerBeat = 60f / VoezEditor.Editor.project.songBPM;
                timeIncrement = secondsPerBeat; // BPM data available; set time snap to match BPM
            } else
                timeIncrement = 1f; // No BPM data; treat time snap as beats per second -- ie: 60 BPM
            float offset = VoezEditor.Editor.songTime - (Mathf.Floor(VoezEditor.Editor.songTime / timeIncrement) * timeIncrement);

            if (offset <= 1f / VoezEditor.Editor.framesPerSecond)
                pulseFlashEffectTime = 5;
        }

        if (VoezEditor.Editor.EditMode && !VoezEditor.Editor.MenuOpen && VoezEditor.Editor.trackEditMode) {
            // Delete Track
            if (activeHover && (Input.GetKeyDown(KeyCode.Delete) || (Util.ShiftDown() && Input.GetMouseButtonDown(1)))) {
                Vector2 windowCenter = new Vector2(VoezEditor.windowRes.x * 0.5f, VoezEditor.windowRes.y * 0.5f);
                int damageCount = 0;
                for(int i=0; i<VoezEditor.Editor.project.notes.Count; i+=1) {
                    if (VoezEditor.Editor.project.notes[i].track == data.id)
                        damageCount += 1;
                }
                if (damageCount > 0) {
                    // There are notes attached to this track. Warn the user before deleting it!
                    deletionConfirm = new ConfirmBox(new Rect(new Vector2(windowCenter.x - 300f, windowCenter.y - 150f), new Vector2(600f, 300f)), "Warning:\nThere are notes attached to this track!\nDeleting it will also delete "+damageCount.ToString()+" note"+(damageCount != 1 ? "s" : "")+".\nAre you sure you want to delete?");
                    VoezEditor.Editor.AddObject(deletionConfirm);
                }
                else {
                    // Empty track, delete it immediately.
                    VoezEditor.Editor.project.DeleteTrack(data.id);
                    VoezEditor.Editor.RefreshAllTracks();
                }
            }
            // Edit Track
            if (activeHover && Input.GetMouseButtonDown(0) && !VoezEditor.Editor.ui.HoveringOverSubmenuItem()) {
                float trackEditWindowX = 0f;
                if (pos.x > VoezEditor.windowRes.x * 0.5f)
                    trackEditWindowX = pos.x - TrackEditor.WIDTH * 0.5f - 64f;
                else
                    trackEditWindowX = pos.x + TrackEditor.WIDTH * 0.5f + 64f;
                VoezEditor.Editor.trackEditor = new TrackEditor(new Vector2(trackEditWindowX, VoezEditor.windowRes.y * 0.55f), data);
                VoezEditor.Editor.AddObject(VoezEditor.Editor.trackEditor);
                VoezEditor.Editor.ui.bpmButton.toggled = false;
            }
        }

        if (deletionConfirm != null) {
            if (deletionConfirm.yesButton != null && deletionConfirm.yesButton.clicked) {
                deletionConfirm.Destroy();
                deletionConfirm = null;
                VoezEditor.Editor.project.DeleteTrack(data.id);
                VoezEditor.Editor.RefreshAllNotes();
                VoezEditor.Editor.RefreshAllTracks();
            }
            else if (deletionConfirm.noButton != null && deletionConfirm.noButton.clicked) {
                deletionConfirm.Destroy();
                deletionConfirm = null;
            }
        }
    }

    // Move Tweening
    public float GetXAtTime(float time)
    {
        float desiredX = data.x;
        float lastSubMove = data.x;
        for (int i = 0; i < data.move.Count; i += 1) {
            if (time >= data.move[i].start && time < data.move[i].end) {
                if (i == 0)
                    lastSubMove = data.x;
                else
                    lastSubMove = data.move[i - 1].to;
                float subMoveProgress = (time - data.move[i].start) / (data.move[i].end - data.move[i].start);
                desiredX = data.move[i].GetEaseFunction()(lastSubMove, data.move[i].to, Mathf.Clamp(subMoveProgress, 0, 1));
            } else if ((i < data.move.Count - 1 && time >= data.move[i].end && time < data.move[i + 1].start)
                   || ((i == data.move.Count - 1 && time >= data.move[i].end && time < data.end)))
                desiredX = data.move[i].to;
        }
        return desiredX;
    }

    public int Spr_BackGradient { get { return 0; } }
    public int Spr_LeftGradLine { get { return 2; } }
    public int Spr_RightGradLine { get { return 3; } }
    public int Spr_MiddleGradLine { get { return 4; } }
    public int Spr_BottomGradFade { get { return 1; } }
    public int Spr_BottomDiamond { get { return 5; } }

    public override void InitiateSprites(SpriteGroup sGroup)
    {
        sGroup.sprites = new FSprite[6];
        sGroup.sprites[Spr_BackGradient] = new FSprite("trackGradient");
        sGroup.sprites[Spr_BackGradient].scaleY = (VoezEditor.windowRes.y / sGroup.sprites[Spr_BackGradient].height) * TRACK_SCREEN_HEIGHT;
        sGroup.sprites[Spr_BackGradient].scaleX = (VoezEditor.windowRes.x / sGroup.sprites[Spr_BackGradient].width) * TRACK_SCREEN_WIDTH * data.size;
        sGroup.sprites[Spr_BackGradient].color = ProjectData.colors[data.color];
        sGroup.sprites[Spr_BackGradient].anchorY = 0f;
        sGroup.sprites[Spr_BackGradient].alpha = 0.9f;

        sGroup.sprites[Spr_BottomGradFade] = new FSprite("bottomGradient");
        sGroup.sprites[Spr_BottomGradFade].scaleY = 1f;
        sGroup.sprites[Spr_BottomGradFade].scaleX = (VoezEditor.windowRes.x / sGroup.sprites[Spr_BottomGradFade].width) * TRACK_SCREEN_WIDTH * data.size;
        sGroup.sprites[Spr_BottomGradFade].color = Color.white;
        sGroup.sprites[Spr_BottomGradFade].anchorY = 0f;
        sGroup.sprites[Spr_BottomGradFade].alpha = 1f;

        for (int i=Spr_LeftGradLine; i<=Spr_MiddleGradLine; i+=1) {
            sGroup.sprites[i] = new FSprite("trackGradient");
            sGroup.sprites[i].scaleY = sGroup.sprites[Spr_BackGradient].scaleY;
            sGroup.sprites[i].scaleX = 0.5f;
            if (i == Spr_MiddleGradLine)
                sGroup.sprites[i].color = Color.black;
            else
                sGroup.sprites[i].color = Color.white;
            sGroup.sprites[i].anchorY = 0f;
        }

        sGroup.sprites[Spr_BottomDiamond] = new FSprite("slide");
        sGroup.sprites[Spr_BottomDiamond].color = Color.black;
        sGroup.sprites[Spr_BottomDiamond].rotation = 45f;
        sGroup.sprites[Spr_BottomDiamond].scale = 0.5f;
    }

    public override void AddToContainer(SpriteGroup sGroup, FContainer newContainer)
    {
        foreach (FSprite fsprite in sGroup.sprites)
            fsprite.RemoveFromContainer();
        VoezEditor.Editor.tracksBottomContainer.AddChild(sGroup.sprites[Spr_BackGradient]);
        VoezEditor.Editor.tracksBottomContainer.AddChild(sGroup.sprites[Spr_BottomGradFade]);
        VoezEditor.Editor.tracksTopContainer.AddChild(sGroup.sprites[Spr_LeftGradLine]);
        VoezEditor.Editor.tracksTopContainer.AddChild(sGroup.sprites[Spr_RightGradLine]);
        VoezEditor.Editor.tracksTopContainer.AddChild(sGroup.sprites[Spr_MiddleGradLine]);
        VoezEditor.Editor.tracksTopContainer.AddChild(sGroup.sprites[Spr_BottomDiamond]);
    }

    public override void DrawSprites(SpriteGroup sGroup, float frameProgress)
    {
        if (lastPos == Vector2.zero || pos == Vector2.zero) {
            for(int i=0; i<sGroup.sprites.Length; i+=1)
                sGroup.sprites[i].isVisible = false;
        } else {
            for (int i = 0; i < sGroup.sprites.Length; i += 1)
                sGroup.sprites[i].isVisible = true;
        }

        // Color Tweening
        int lastSubColor = data.color;
        bool appliedColor = false;
        for (int i = 0; i < data.colorChange.Count; i += 1) {
            if (VoezEditor.Editor.songTime >= data.colorChange[i].start && VoezEditor.Editor.songTime < data.colorChange[i].end) {
                if (i == 0)
                    lastSubColor = data.color;
                else
                    lastSubColor = (int)data.colorChange[i - 1].to;
                float subColorProgress = (VoezEditor.Editor.songTime - data.colorChange[i].start) / (data.colorChange[i].end - data.colorChange[i].start);
                sGroup.sprites[Spr_BackGradient].color = Color.Lerp(ProjectData.colors[lastSubColor], ProjectData.colors[(int)data.colorChange[i].to], data.colorChange[i].GetEaseFunction()(0f, 1f, Mathf.Clamp(subColorProgress, 0, 1)));
                appliedColor = true;
            } else if ((i < data.colorChange.Count - 1 && VoezEditor.Editor.songTime >= data.colorChange[i].end && VoezEditor.Editor.songTime < data.colorChange[i + 1].start)
                   || ((i == data.colorChange.Count - 1 && VoezEditor.Editor.songTime >= data.colorChange[i].end && VoezEditor.Editor.songTime <= data.end))) {
                sGroup.sprites[Spr_BackGradient].color = ProjectData.colors[(int)data.colorChange[i].to];
                appliedColor = true;
            }
        }
        if (!appliedColor)
            sGroup.sprites[Spr_BackGradient].color = ProjectData.colors[lastSubColor];

        // Scale Tweening
        currentWidth = VoezEditor.windowRes.x * TRACK_SCREEN_WIDTH * data.size;
        float lastSubScale = data.size;
        for (int i = 0; i < data.scale.Count; i += 1) {
            if (VoezEditor.Editor.songTime >= data.scale[i].start && VoezEditor.Editor.songTime < data.scale[i].end) {
                if (i == 0)
                    lastSubScale = data.size;
                else
                    lastSubScale = data.scale[i - 1].to;
                float subScaleProgress = (VoezEditor.Editor.songTime - data.scale[i].start) / (data.scale[i].end - data.scale[i].start);
                currentWidth = VoezEditor.windowRes.x * TRACK_SCREEN_WIDTH;
                currentWidth *= data.scale[i].GetEaseFunction()(lastSubScale, data.scale[i].to, Mathf.Clamp(subScaleProgress, 0, 1));
            } else if ((i < data.scale.Count - 1 && VoezEditor.Editor.songTime >= data.scale[i].end && VoezEditor.Editor.songTime < data.scale[i + 1].start)
                   || ((i == data.scale.Count - 1 && VoezEditor.Editor.songTime >= data.scale[i].end && VoezEditor.Editor.songTime <= data.end)))
                currentWidth = VoezEditor.windowRes.x * TRACK_SCREEN_WIDTH * data.scale[i].to;
        }
        sGroup.sprites[Spr_BackGradient].scaleX = currentWidth / sGroup.sprites[Spr_BackGradient].element.sourceRect.width;
        sGroup.sprites[Spr_BottomGradFade].scaleX = (currentWidth-14f) / (sGroup.sprites[Spr_BottomGradFade].element.sourceRect.width);

        Vector2 lerpPos = new Vector2(Mathf.Lerp(lastPos.x, pos.x, frameProgress), Mathf.Lerp(lastPos.y, pos.y, frameProgress));
        sGroup.sprites[Spr_BackGradient].x = lerpPos.x;
        sGroup.sprites[Spr_LeftGradLine].x = lerpPos.x - currentWidth * 0.5f + 7f;
        sGroup.sprites[Spr_RightGradLine].x = lerpPos.x + currentWidth * 0.5f - 7f;
        sGroup.sprites[Spr_MiddleGradLine].x = lerpPos.x;
        sGroup.sprites[Spr_BottomGradFade].x = lerpPos.x;
        sGroup.sprites[Spr_BottomDiamond].x = lerpPos.x;
        for (int i = 0; i < sGroup.sprites.Length; i += 1)
            sGroup.sprites[i].y = lerpPos.y;

        if (flashEffectTime > 0 || pulseFlashEffectTime > 0) {
            sGroup.sprites[Spr_BottomDiamond].scale = 1.25f;
        }
        else {
            sGroup.sprites[Spr_BottomDiamond].scale = 0.5f;
            sGroup.sprites[Spr_BottomDiamond].color = Color.black;
        }

        if (activeHover || (VoezEditor.Editor.trackEditor != null && VoezEditor.Editor.trackEditor.data.id == ID)) {
            sGroup.sprites[Spr_MiddleGradLine].color = Color.red;
            sGroup.sprites[Spr_BottomDiamond].color = Color.red;
            if (VoezEditor.Editor.trackEditMode) {
                sGroup.sprites[Spr_LeftGradLine].color = Color.red;
                sGroup.sprites[Spr_RightGradLine].color = Color.red;
            }
            sGroup.sprites[Spr_BackGradient].alpha = 1f;
        } else {
            sGroup.sprites[Spr_MiddleGradLine].color = Color.black;
            sGroup.sprites[Spr_BottomDiamond].color = Color.black;
            sGroup.sprites[Spr_LeftGradLine].color = Color.white;
            sGroup.sprites[Spr_RightGradLine].color = Color.white;
            if (VoezEditor.Editor.trackEditor != null)
                sGroup.sprites[Spr_BackGradient].alpha = 0.25f;
            else
                sGroup.sprites[Spr_BackGradient].alpha = 0.9f;
        }

        if (pulseFlashEffectTime > 0)
            sGroup.sprites[Spr_BottomDiamond].color = Color.white;

        base.DrawSprites(sGroup, frameProgress);
    }
}
