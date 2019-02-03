using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Random = UnityEngine.Random;
using System.Text.RegularExpressions;

public class left_and_rightScript : MonoBehaviour
{

    public KMAudio Audio;
    public KMBombInfo Bomb;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    public KMSelectable leftButton;
    public KMSelectable rightButton;
    public GameObject sequenceText;

    private int leftPressed = 0;
    private int rightPressed = 0;
    private int timesPressed = 0;

    private Color32 blue = new Color32(20, 16, 213, 255);
    private Color32 green = new Color32(32, 243, 70, 255);

    private int leftSwitch;
    private int rightSwitch;

    private string sequence;
    private string lrSequence;
    private string sequenceTxt = "";

    struct Condition
    {
        public string Explanation;
        public Func<KMBombInfo, bool> Eval;
        public int Value;
        public int FailSwitch;
    }

    private static readonly Condition[] switchCondition = new Condition[]
    {
        new Condition { Explanation = "More lit than unlit indicators", Eval = bomb => bomb.GetOnIndicators().Count() > bomb.GetOffIndicators().Count(), Value = 3 },
        new Condition { Explanation = "A vowel in the serial number", Eval = bomb => bomb.GetSerialNumberLetters().Any(x => x == 'A' || x == 'E' || x == 'I' || x == 'O' || x == 'U'), Value = 2 },
        new Condition { Explanation = "Battery holders + port plates + indicators <= 5", Eval = bomb => bomb.GetBatteryHolderCount() + bomb.GetPortPlateCount() + bomb.GetOffIndicators().Count() + bomb.GetOnIndicators().Count() <= 5, Value = 4},
        new Condition { Explanation = "At least 5 modules on the bomb", Eval = bomb => bomb.GetSolvableModuleNames().Count() >= 5, Value = 1 },
        new Condition { Explanation = "Numbers in S# equal to letters in S#", Eval = bomb => bomb.GetSerialNumberLetters().Count() == bomb.GetSerialNumberNumbers().Count(), Value = 2 },
        new Condition { Explanation = "No condition applied", Eval = bomb => true, Value = 3, FailSwitch = 1 }
    };

    void Awake()
    {
        leftButton.OnInteract += delegate { Press("0");  return false; };
        rightButton.OnInteract += delegate { Press("1"); return false; };
        moduleId = moduleIdCounter++;
    }

    void Start()
    {
        CalculateSequence();
        MixButtons();
        SetSwitch();
    }

    void CalculateSequence()
    {
        sequence += Math.Pow(Bomb.GetSerialNumberNumbers().Last(), 3) % 8;
        if (Bomb.GetPortCount() != Bomb.GetPortPlateCount())
            sequence += Math.Pow(Bomb.GetPortCount() + Bomb.GetPortPlateCount(), 4) % 4;
        else
        {
            sequence += Bomb.GetBatteryCount(Battery.D);
            sequence += Bomb.GetSerialNumberLetters().Count();
        }
        if (Bomb.GetBatteryCount() == Bomb.GetBatteryHolderCount())
            sequence += Math.Pow(Bomb.GetOnIndicators().Count() - Bomb.GetOffIndicators().Count(), 2) % 6;
        else
        {
            sequence += Bomb.GetBatteryCount() + Bomb.GetBatteryHolderCount();
        }
        Debug.LogFormat(@"[Left and Right #{0}] The calculated sequence from edgework is: {1}", moduleId, sequence);
        sequence = Convert.ToString(Convert.ToInt32(sequence, 10), 2);
        lrSequence = sequence;
        lrSequence = lrSequence.Replace("0", "L,");
        lrSequence = lrSequence.Replace("1", "R,");
        Debug.LogFormat(@"[Left and Right #{0}] The converted sequence is: {1}", moduleId, lrSequence);
    }

    void MixButtons()
    {
        if (Random.Range(0, 10) < 5)
        {
            leftButton.GetComponent<MeshRenderer>().material.color = green;
            rightButton.GetComponent<MeshRenderer>().material.color = blue;
        }
        else
        {
            leftButton.GetComponent<MeshRenderer>().material.color = blue;
            rightButton.GetComponent<MeshRenderer>().material.color = green;
        }
    }

    void SetSwitch()
    {
        if( Bomb.IsIndicatorOn(Indicator.FRK) && Bomb.IsIndicatorOff(Indicator.NSA) && Bomb.IsPortPresent(Port.PS2) && Bomb.IsPortPresent(Port.Parallel) && Bomb.IsPortPresent(Port.Serial) && Bomb.IsPortPresent(Port.RJ45) && Bomb.IsPortPresent(Port.DVI) && !Bomb.IsPortPresent(Port.StereoRCA))
        {
            leftSwitch = rightSwitch = -1;
            Debug.LogFormat(@"[Left and Right #{0}] The Special rule applied, lucker. No switches will occur", moduleId);
            return;
        }

        if (switchCondition.First(cond => cond.Eval(Bomb)).FailSwitch == 1)
        {
            var fsSwitch = switchCondition.First(cond => cond.FailSwitch == 1).Value;
            var fsSwitchExplanation = switchCondition.First(cond => cond.FailSwitch == 1).Explanation;
            leftSwitch = fsSwitch;
            rightSwitch = fsSwitch;
            Debug.LogFormat(@"[Left and Right #{0}] The green button will incur a switch after {1} presses.  Reason: {2}", moduleId, leftSwitch, fsSwitchExplanation);
            Debug.LogFormat(@"[Left and Right #{0}] The blue button will incur a switch after {1} presses.  Reason: {2}", moduleId, rightSwitch, fsSwitchExplanation);
            return;
        }

        var allValues = switchCondition.Where(cond => cond.Eval(Bomb)).Select(cond => cond.Value).ToArray();
        var allExplanations = switchCondition.Where(cond => cond.Eval(Bomb)).Select(cond => cond.Explanation).ToArray();
        if (leftButton.GetComponent<MeshRenderer>().material.color == green)
        {
            leftSwitch = allValues[0];
            rightSwitch = allValues[1];
            var leftExplanation = allExplanations[0];
            var rightExplanation = allExplanations[1];
            Debug.LogFormat(@"[Left and Right #{0}] The green button will incur a switch after {1} presses.  Reason: {2}", moduleId, leftSwitch, leftExplanation);
            Debug.LogFormat(@"[Left and Right #{0}] The blue button will incur a switch after {1} presses.  Reason: {2}", moduleId, rightSwitch, rightExplanation);
        }
        else
        {
            leftSwitch = allValues[1];
            rightSwitch = allValues[0];
            var leftExplanation = allExplanations[1];
            var rightExplanation = allExplanations[0];
            Debug.LogFormat(@"[Left and Right #{0}] The green button will incur a switch after {1} presses.  Reason: {2}", moduleId, rightSwitch, rightExplanation);
            Debug.LogFormat(@"[Left and Right #{0}] The blue button will incur a switch after {1} presses.  Reason: {2}", moduleId, leftSwitch, leftExplanation);
        }
    }
    
    void Press(string leftRight)
    {
        if (leftRight == "0")
        {
            leftButton.AddInteractionPunch();
            if (sequence[0].ToString() == "0")
            {
                if (sequence.Length == 1)
                {
                    sequenceTxt += "L";
                    sequenceText.GetComponent<TextMesh>().text = sequenceTxt;
                    GetComponent<KMBombModule>().HandlePass();
                    moduleSolved = true;
                    return;
                }

                sequence = sequence.Remove(0, 1);
                leftPressed++;
                sequenceTxt += "L";
                sequenceText.GetComponent<TextMesh>().text = sequenceTxt;
                if (leftPressed == leftSwitch)
                {
                    SwitchLR(leftRight);
                    leftPressed = 0;
                }
            }
            else
            {
                GetComponent<KMBombModule>().HandleStrike();
            }
        }
        else
        {
            rightButton.AddInteractionPunch();

            if (sequence[0].ToString() == "1")
            {
                if (sequence.Length == 1)
                {
                    sequenceTxt += "R";
                    sequenceText.GetComponent<TextMesh>().text = sequenceTxt;
                    GetComponent<KMBombModule>().HandlePass();
                    moduleSolved = true;
                    return;
                }
                    
                sequence = sequence.Remove(0, 1);
                rightPressed++;
                sequenceTxt += "R";
                sequenceText.GetComponent<TextMesh>().text = sequenceTxt;
                if (rightPressed == rightSwitch)
                {
                    SwitchLR(leftRight);
                    rightPressed = 0;
                }
            }
            else
            {
                GetComponent<KMBombModule>().HandleStrike();
            }
        }
    }

   void SwitchLR(string LR)
    {
        sequence = sequence.Replace("0", "2");
        sequence = sequence.Replace("1", "0");
        sequence = sequence.Replace("2", "1");

        lrSequence = sequence;

        lrSequence = lrSequence.Replace("0", "L,");
        lrSequence = lrSequence.Replace("1", "R,");

        if (LR == "0")
            Debug.LogFormat(@"[Left and Right #{0}] The left button caused a switch after {1} presses. Remaining Sequence: {2}", moduleId, leftSwitch, lrSequence);
        else
            Debug.LogFormat(@"[Left and Right #{0}] The right button caused a switch after {1} presses. Remaining Sequence: {2}", moduleId, rightSwitch, lrSequence);
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} left [press the left button] | !{0} right [press the right button]";
#pragma warning restore 0414

    private List<KMSelectable> ProcessTwitchCommand(string command)
    {
        var list = new List<KMSelectable>();
        Match m;

        if ((m = Regex.Match(command, @"^\s*(left|l)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            list.Add(leftButton);
            Debug.LogFormat(@"[Left and Right #{0}] TwitchPlays pressed the left button", moduleId);

            return list;
        }

        if ((m = Regex.Match(command, @"^\s*(right|r)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            list.Add(rightButton);
            Debug.LogFormat(@"[Left and Right #{0}] TwitchPlays pressed the right button", moduleId);

            return list;
        }


        return null;
    }
}

