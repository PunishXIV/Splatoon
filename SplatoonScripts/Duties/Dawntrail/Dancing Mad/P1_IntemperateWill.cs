using System.Collections.Generic;
using ECommons.GameFunctions;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dmad;

internal class Dmad_P1_IntemperateWill : SplatoonScript
{
    #region Metadata

    public override Metadata? Metadata => new(1, "mirage");
    public override HashSet<uint>? ValidTerritories => [TerritoryDmad];

    #endregion

    #region Constant

    private const uint TerritoryDmad = 1363;
    private const int SceneIntemperateWill = 4;
    
    private const uint DataIdLeftHalf = 2015164;
    private const uint DataIdRightHalf = 2015165;

    private const uint ObjectEffectEnableData1 = 64;
    private const uint ObjectEffectEnableData2 = 128;
    private const uint ObjectEffectDisableData1 = 256;
    private const uint ObjectEffectDisableData2 = 512;

    private const string ElLeftHalf = "Lefthalf";
    private const string ElRightHalf = "Righthalf";

    #endregion

    #region State

    private bool _leftHalfEnabled;
    private bool _rightHalfEnabled;

    #endregion

    #region LifeCycle

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode(ElLeftHalf,
            """{"Name":"Lefthalf","type":5,"Enabled":false,"refX":100.0,"refY":100.0,"radius":20.0,"coneAngleMax":180,"includeRotation":true}""",
            overwrite: true);
        Controller.RegisterElementFromCode(ElRightHalf,
            """{"Name":"Righthalf","type":5,"Enabled":false,"refX":100.0,"refY":100.0,"radius":20.0,"coneAngleMin":180,"coneAngleMax":360,"includeRotation":true}""",
            overwrite: true);
    }

    public override void OnUpdate()
    {
        if (Controller.Scene != SceneIntemperateWill)
        {
            Controller.Hide();
            return;
        }

        if (Controller.TryGetElementByName(ElLeftHalf, out var leftHalf))
            leftHalf.Enabled = _leftHalfEnabled;

        if (Controller.TryGetElementByName(ElRightHalf, out var rightHalf))
            rightHalf.Enabled = _rightHalfEnabled;
    }

    public override void OnObjectEffect(uint target, uint data1, uint data2)
    {
        if (Controller.Scene != SceneIntemperateWill) return;
        if (!target.TryGetObject(out var obj)) return;

        if (obj.DataId == DataIdLeftHalf)
        {
            if (data1 == ObjectEffectEnableData1 && data2 == ObjectEffectEnableData2)
                _leftHalfEnabled = true;
            else if (data1 == ObjectEffectDisableData1 && data2 == ObjectEffectDisableData2)
                _leftHalfEnabled = false;
        }
        else if (obj.DataId == DataIdRightHalf)
        {
            if (data1 == ObjectEffectEnableData1 && data2 == ObjectEffectEnableData2)
                _rightHalfEnabled = true;
            else if (data1 == ObjectEffectDisableData1 && data2 == ObjectEffectDisableData2)
                _rightHalfEnabled = false;
        }
    }

    public override void OnReset()
    {
        _leftHalfEnabled = false;
        _rightHalfEnabled = false;
        Controller.Hide();
    }

    #endregion
}
