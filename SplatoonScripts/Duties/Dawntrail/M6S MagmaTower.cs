using ECommons;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
internal class M6S_MagmaTower :SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1259];
    public override Metadata? Metadata => new(1, "Redmoon");

    private enum State
    {
        None,
        Active,
        Cone1,
        Elaption1,
        TowerIn1,
        WingPosSearch,
        Wing,
        Elaption2,
        TowerIn2,
    }

    // 塔位置 北西 41 ~ 50
    private readonly uint[] TowerNW = { 41, 42, 43, 44, 45, 46, 47, 48, 49, 50 };

    // 塔位置 北東 51 ~ 60
    private readonly uint[] TowerNE = { 51, 52, 53, 54, 55, 56, 57, 58, 59, 60 };

    // 塔位置 南 61 ~ 68
    private readonly uint[] TowerS = { 61, 62, 63, 64, 65, 66, 67, 68 };

    private State _state = State.None;
    private int _towerCountNE = 0;
    private int _towerCountNW = 0;
    private int _towerCountS = 0;

    public override void OnSetup()
    {
        Element text = new Element(1);
        text.refActorComparisonType = 2;
        text.overlayText = "まだ塔に入らない";
        text.overlayVOffset = 0.5f;
        text.overlayFScale = 2.0f;
        text.overlayTextColor = 0xFF0000FF;
        Controller.RegisterElement("Text", text);

        Controller.RegisterElementFromCode("Line1Normal", "{\"Name\":\"\",\"type\":2,\"refX\":82.0,\"refY\":90.7,\"refZ\":-3.8146973E-06,\"offX\":117.0,\"offY\":90.7,\"radius\":0.0,\"color\":3356335872,\"fillIntensity\":0.345,\"thicc\":15.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("Line1_8Tower", "{\"Name\":\"\",\"type\":2,\"refX\":82.0,\"refY\":117.0,\"refZ\":-3.8146973E-06,\"offX\":117.0,\"offY\":117.0,\"radius\":0.0,\"color\":3356335872,\"fillIntensity\":0.345,\"thicc\":15.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("Line2", "{\"Name\":\"\",\"type\":2,\"refX\":85.0,\"refY\":118.0,\"refZ\":-1.9073486E-06,\"offX\":85.0,\"offY\":83.0,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":15.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("Line3", "{\"Name\":\"\",\"type\":2,\"refX\":115.0,\"refY\":82.0,\"refZ\":5.722046E-06,\"offX\":115.0,\"offY\":117.0,\"radius\":0.0,\"color\":3355508223,\"fillIntensity\":0.345,\"thicc\":15.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("LineClock1", "{\"Name\":\"\",\"type\":2,\"refX\":117.0,\"refY\":90.7,\"offX\":110.0,\"offY\":89.0,\"radius\":0.0,\"color\":3356335872,\"fillIntensity\":0.345,\"thicc\":15.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("LineClock2", "{\"Name\":\"\",\"type\":2,\"refX\":85.0,\"refY\":83.0,\"offX\":84.0,\"offY\":86.92,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":15.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("LineClock3", "{\"Name\":\"\",\"type\":2,\"refX\":118.5,\"refY\":113.88,\"refZ\":5.722046E-06,\"offX\":115.0,\"offY\":117.0,\"radius\":0.0,\"color\":3355508223,\"fillIntensity\":0.345,\"thicc\":15.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("LineCounterClock1", "{\"Name\":\"\",\"type\":2,\"refX\":82.0,\"refY\":90.7,\"offX\":84.6,\"offY\":89.18,\"radius\":0.0,\"color\":3356335872,\"fillIntensity\":0.345,\"thicc\":15.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("LineCounterClock2", "{\"Name\":\"\",\"type\":2,\"refX\":85.0,\"refY\":118.0,\"offX\":82.4,\"offY\":111.98,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":15.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("LineCounterClock3", "{\"Name\":\"\",\"type\":2,\"refX\":115.0,\"refY\":82.0,\"refZ\":5.722046E-06,\"offX\":112.54,\"offY\":85.7,\"radius\":0.0,\"color\":3355508223,\"fillIntensity\":0.345,\"thicc\":15.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("Line8Sorth", "{\"Name\":\"\",\"type\":2,\"refX\":82.0,\"refY\":117.0,\"offX\":86.46,\"offY\":118.92,\"radius\":0.0,\"color\":3356335872,\"fillIntensity\":0.345,\"thicc\":15.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");

        Element[] towers = new Element[8];
        for (int i = 0; i < towers.Length; i++)
        {
            towers[i] = new Element(0);
            towers[i].radius = 3f;
            towers[i].Filled = false;
            towers[i].thicc = 2f;
            towers[i].overlayText = $"Tower{i}";
        }
        towers[0].SetRefPosition(new Vector3(85f, 0f, 114f));
        towers[0].overlayText = "D4";
        towers[1].SetRefPosition(new Vector3(91f, 0f, 117f));
        towers[1].overlayText = "D3";
        towers[2].SetRefPosition(new Vector3(92f, 0f, 110f));
        towers[2].overlayText = "H1";
        towers[3].SetRefPosition(new Vector3(98f, 0f, 117f));
        towers[3].overlayText = "H2";
        towers[4].SetRefPosition(new Vector3(100f, 0f, 108f));
        towers[4].overlayText = "ST";
        towers[5].SetRefPosition(new Vector3(107f, 0f, 111f));
        towers[5].overlayText = "D1";
        towers[6].SetRefPosition(new Vector3(105f, 0f, 117f));
        towers[6].overlayText = "D2";
        towers[7].SetRefPosition(new Vector3(112f, 0f, 116f));
        towers[7].overlayText = "MT";
        for (int i = 0; i < towers.Length; i++)
        {
            Controller.RegisterElement($"Tower{i}", towers[i]);
        }
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 42679u && _state == State.Active)
        {
            _state = State.Cone1;
        }
        if (castId == 42614u && _state == State.TowerIn1)
        {
            _state = State.WingPosSearch;
        }
        if (castId == 42653u && _state == State.Elaption1)
        {
            _state = State.TowerIn1;
        }
        if (castId == 42653u && _state == State.Elaption2)
        {
            _state = State.TowerIn2;
        }
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if (_state != State.WingPosSearch) return;
        if (data1 != (ushort)1u || data2 != (ushort)2u) return;

        // positionと配列の値を比較して、どこの塔かを判定しカウント
        if (TowerNW.Contains(position))
        {
            _towerCountNW++;
        }
        else if (TowerNE.Contains(position))
        {
            _towerCountNE++;
        }
        else if (TowerS.Contains(position))
        {
            _towerCountS++;
        }

        // カウント合計が８以上になったら、次処理へ
        if (_towerCountNE + _towerCountNW + _towerCountS >= 8)
        {
            _state = State.Wing;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (!set.Action.HasValue) return;
        if (set.Action.Value.RowId == 42683u)
        {
            _state = State.Elaption1;
        }
        if (set.Action.Value.RowId == 42659u && _state == State.TowerIn2)
        {
            this.OnReset();
        }
    }

    public override void OnRemoveBuffEffect(uint sourceId, Status Status)
    {
        if (_state == State.None) return;
        if (sourceId == Player.Object.EntityId && Status.StatusId == 4450)
        {
            _state = State.Elaption2;
        }
    }

    public override void OnMessage(string Message)
    {
        if (Message.Contains("シュガーライオットが暴走！ 火山からマグマが流れてきたァ～！"))
        {
            _state = State.Active;
        }
    }

    public override void OnSettingsDraw()
    {
        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGuiEx.Text($"State: {_state}");
            ImGuiEx.Text($"TowerCountNE: {_towerCountNE}");
            ImGuiEx.Text($"TowerCountNW: {_towerCountNW}");
            ImGuiEx.Text($"TowerCountS: {_towerCountS}");
        }
    }

    public override void OnReset()
    {
        _state = State.None;
        _towerCountNE = 0;
        _towerCountNW = 0;
        _towerCountS = 0;
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    public override void OnUpdate()
    {
        if (_state == State.None) return;
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        switch (_state)
        {
            case State.Cone1:
                // No Operation
                break;
            case State.Elaption1:
                if (Controller.TryGetElementByName("Text", out var text))
                {
                    text.refActorObjectID = Player.Object.EntityId;
                    text.Enabled = true;
                }
                break;
            case State.WingPosSearch:
                // No Operation
                break;
            case State.Wing:
                if (!Controller.TryGetElementByName("Line1Normal", out var line1)) break;
                if (!Controller.TryGetElementByName("Line1_8Tower", out var line1_8Tower)) break;
                if (!Controller.TryGetElementByName("Line2", out var line2)) break;
                if (!Controller.TryGetElementByName("Line3", out var line3)) break;
                if (!Controller.TryGetElementByName("LineClock1", out var lineClock1)) break;
                if (!Controller.TryGetElementByName("LineClock2", out var lineClock2)) break;
                if (!Controller.TryGetElementByName("LineClock3", out var lineClock3)) break;
                if (!Controller.TryGetElementByName("LineCounterClock1", out var lineCounterClock1)) break;
                if (!Controller.TryGetElementByName("LineCounterClock2", out var lineCounterClock2)) break;
                if (!Controller.TryGetElementByName("LineCounterClock3", out var lineCounterClock3)) break;
                if (!Controller.TryGetElementByName("Line8Sorth", out var line8Sorth)) break;
                // 北西４塔
                if (_towerCountNW == 4)
                {
                    line1.Enabled = true;
                    line2.Enabled = true;
                    line3.Enabled = true;
                    lineClock1.Enabled = true;
                    lineClock2.Enabled = true;
                    lineClock3.Enabled = true;
                }
                // 北東４塔
                if (_towerCountNE == 4)
                {
                    line1.Enabled = true;
                    line2.Enabled = true;
                    line3.Enabled = true;
                    lineCounterClock1.Enabled = true;
                    lineCounterClock2.Enabled = true;
                    lineCounterClock3.Enabled = true;
                }
                // 南８塔
                if (_towerCountS == 8)
                {
                    line1_8Tower.Enabled = true;
                    line2.Enabled = true;
                    line3.Enabled = true;
                    lineClock3.Enabled = true;
                    lineCounterClock2.Enabled = true;
                    line8Sorth.Enabled = true;
                }
                if (_towerCountS != 8) break;
                for (int i = 0; i < 8; i++)
                {
                    if (Controller.TryGetElementByName($"Tower{i}", out var tower))
                    {
                        tower.Enabled = true;
                    }
                }
                break;
            case State.Elaption2:
                if (Controller.TryGetElementByName("Text", out var text2))
                {
                    text2.refActorObjectID = Player.Object.EntityId;
                    text2.Enabled = true;
                }
                if (_towerCountS != 8) break;
                for (int i = 0; i < 8; i++)
                {
                    if (Controller.TryGetElementByName($"Tower{i}", out var tower))
                    {
                        tower.Enabled = true;
                    }
                }
                break;
            case State.TowerIn2:
                if (_towerCountS != 8) break;
                for (int i = 0; i < 8; i++)
                {
                    if (Controller.TryGetElementByName($"Tower{i}", out var tower))
                    {
                        tower.Enabled = true;
                    }
                }
                break;
            default:
                break;
        }
    }


}
