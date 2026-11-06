using System;
using FireSafety.Gameplay;
using FireSafety.Equipment;

public static class TutorialProgressTests
{
    private static int checks;
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); checks++; }

    public static void Main()
    {
        var progress = new TutorialProgress();
        Check(progress.Step == TutorialStep.Welcome && progress.NeedsBriefing, "Tutorial must begin with a briefing");
        Check(!progress.Observe(TutorialAction.Escaped), "An early escape must not complete the tutorial");
        Check(progress.Answer(0) == 0, "Information cards must not award quiz points");
        Check(progress.ContinueBriefing() && progress.Step == TutorialStep.Movement, "Welcome must lead to movement");
        Check(!progress.Observe(TutorialAction.Moved), "Practice must wait for its briefing");
        Check(progress.ContinueBriefing() && !progress.NeedsBriefing, "Practice button must enable the exercise");
        Check(!progress.ContinueBriefing(), "Continue must not skip hands-on movement");
        Check(!progress.Observe(TutorialAction.WindowOpened), "Another action must not satisfy movement");
        Check(progress.Observe(TutorialAction.Moved) && progress.Step == TutorialStep.FireClasses, "Actual movement must advance");
        Check(!progress.Observe(TutorialAction.Moved), "Repeating an action must not skip the next lesson");
        Check(progress.ContinueBriefing() && progress.Step == TutorialStep.OpenWindow, "Class explanation must precede window exercise");

        var steps = new[] { TutorialStep.OpenWindow, TutorialStep.CloseWindow, TutorialStep.OpenDoor, TutorialStep.CloseDoor,
            TutorialStep.PickUpBucket, TutorialStep.FillBucket, TutorialStep.SwapForExtinguisher, TutorialStep.RetrieveWater };
        var actions = new[] { TutorialAction.WindowOpened, TutorialAction.WindowClosed, TutorialAction.DoorOpened, TutorialAction.DoorClosed,
            TutorialAction.BucketPickedUp, TutorialAction.BucketFilled, TutorialAction.SwappedForExtinguisher, TutorialAction.WaterRetrieved };
        for (int i = 0; i < steps.Length; i++)
        {
            Check(progress.Step == steps[i], "Hands-on lessons must stay in order");
            Check(!progress.Observe(actions[i]), "Exercises must be explained before completion");
            Check(progress.ContinueBriefing(), "Each exercise must be startable");
            Check(!progress.Observe(TutorialAction.None), "Idle frames must not complete a lesson");
            Check(progress.Observe(actions[i]), "The requested world action must complete its exercise");
        }
        Check(progress.Step == TutorialStep.SmallFireSafety && progress.ContinueBriefing(), "Teach the size limit before extinguishing");
        foreach (TutorialAction action in new[] { TutorialAction.ExtinguishedA, TutorialAction.ExtinguishedB, TutorialAction.ExtinguishedF })
        {
            Check(progress.ContinueBriefing(), "A/B/F fire drills must start after their instructions");
            Check(!progress.Observe(TutorialAction.Escaped), "Escaping must not pass an extinguishing drill");
            Check(progress.Observe(action), "Putting out the matching fire must advance the drill");
        }
        Check(progress.Step == TutorialStep.GasSafety, "Teach gas isolation after the extinguisher drills");
        Check(!progress.ContinueBriefing(), "Gas question must require an answer");
        Check(progress.Answer(-1) == 0 && progress.Answer(3) == 0, "Invalid choices must not score");
        Check(progress.Answer(0) == -5 && progress.Step == TutorialStep.GasSafety, "An unsafe gas answer must deduct points and stay on the question");
        Check(progress.Answer(1) == 10 && progress.Step == TutorialStep.SmokeSafety, "Evacuating a fed gas fire must pass");
        Check(progress.Answer(2) == -5 && progress.Step == TutorialStep.SmokeSafety, "Waiting in smoke must fail");
        Check(progress.Answer(0) == 10 && progress.Step == TutorialStep.SmokePractice, "Getting low and escaping must pass");
        Check(progress.Answer(0) == 0, "Repeating the correct answer must not farm quiz points");
        Check(progress.ContinueBriefing() && progress.Observe(TutorialAction.LeftSmoke), "Smoke drill must precede the final escape");
        Check(progress.Step == TutorialStep.Escape && progress.ContinueBriefing(), "Final escape must be its own live drill");
        Check(!progress.Observe(TutorialAction.ExtinguishedB), "An unrelated extinguish event must not pass the final drill");
        Check(progress.Observe(TutorialAction.Escaped) && progress.IsComplete, "Walking outside must finish the tutorial");
        Check(!progress.ContinueBriefing() && !progress.Observe(TutorialAction.Escaped) && progress.Answer(0) == 0,
            "Completion must be final and unable to award more points");

        foreach (TutorialStep step in Enum.GetValues(typeof(TutorialStep)))
        {
            var retry = new TutorialProgress(step);
            Check(retry.Step == step && retry.NeedsBriefing, "Retry must restore the failed lesson with its instructions");
        }
        foreach (EquipmentKind extinguisher in new[] { EquipmentKind.AbcPowderExtinguisher, EquipmentKind.WetChemicalExtinguisher })
        {
            var swap = new TutorialProgress(TutorialStep.SwapForExtinguisher);
            var water = new EquipmentItem(EquipmentKind.Bucket); water.Fill();
            var tool = new EquipmentItem(extinguisher);
            var inventory = new SingleSlotInventory(); inventory.Swap(water);
            Check(!swap.ObserveEquipment(tool), "A pickup cannot bypass the swap briefing");
            swap.ContinueBriefing();
            Check(inventory.Swap(tool) == water && water.HasWater, "Swapping either extinguisher must leave the full bucket intact");
            Check(swap.ObserveEquipment(inventory.Held) && swap.Step == TutorialStep.RetrieveWater,
                "Choosing wet chemical instead of ABC must still advance to retrieving the bucket");
            Check(TutorialFeedback.Practice(TutorialStep.SwapForExtinguisher, extinguisher).Correct
                == (extinguisher == EquipmentKind.AbcPowderExtinguisher), "The wrong swap must still be identified as incorrect");
            swap.ContinueBriefing(); inventory.Swap(water);
            Check(swap.ObserveEquipment(inventory.Held) && swap.Step == TutorialStep.SmallFireSafety,
                "Both swap paths must continue after retrieving the same water");
        }
        var smoke = new SmokeDrillProgress();
        Check(!smoke.Observe(false, 0f, false), "Starting in clear air must not pass smoke practice");
        Check(!smoke.Observe(false, 1f, false), "Entering smoke alone must not complete practice");
        Check(!smoke.Observe(true, 1f, false) && smoke.HasCoughed, "Coughing must mark the warning, not complete the lesson");
        Check(!smoke.Observe(false, 0.1f, false), "The player must leave smoke, not just move to a lighter cell");
        Check(smoke.Observe(false, 0f, false), "Reaching clear air after coughing must complete smoke practice");
        Check(!smoke.Observe(false, 0f, true), "A dead player cannot complete the smoke drill");
        Check(!new SmokeDrillProgress().Observe(false, 0f, false), "Retry must forget the previous cough");
        Console.WriteLine("Tutorial progression: " + checks + " checks passed.");
    }
}
