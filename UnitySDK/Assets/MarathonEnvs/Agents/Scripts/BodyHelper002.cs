
public static class BodyHelper002
{
    [System.Serializable]
    public enum BodyPartGroup {
        None,
        Hips,
        Torso,
        Spine,
        Head,
        ArmUpper,
        ArmLower,
        Hand,
        LegUpper,
        LegLower,
        Foot,
    }

    [System.Serializable]
    public enum MuscleGroup {
        None,
        Hips,
        Torso,
        Spine,
        Head,
        ArmUpper,
        ArmLower,
        Hand,
        LegUpper,
        LegLower,
        Foot,
    }

    public static BodyPartGroup GetBodyPartGroup(string name)
    {
        name = name.ToLower();
        if (name.Contains("mixamorig"))
            return BodyPartGroup.None;

        if (name.Contains("butt"))
            return BodyPartGroup.Hips;
        if (name.Contains("torso"))
            return BodyPartGroup.Torso;
        if (name.Contains("head"))
            return BodyPartGroup.Head;
        if (name.Contains("waist"))
            return BodyPartGroup.Spine;

        if (name.Contains("thigh"))
            return BodyPartGroup.LegUpper;
        if (name.Contains("shin"))
            return BodyPartGroup.LegLower;
        if (name.Contains("right_right_foot") || name.Contains("left_left_foot"))
            return BodyPartGroup.Foot;
        if (name.Contains("upper_arm"))
            return BodyPartGroup.ArmUpper;
		if (name.Contains("larm"))
            return BodyPartGroup.ArmLower;
		if (name.Contains("hand"))
            return BodyPartGroup.Hand;

        return BodyPartGroup.None;
    }
    public static MuscleGroup GetMuscleGroup(string name)
    {
        name = name.ToLower();
        if (name.Contains("mixamorig"))
            return MuscleGroup.None;
        if (name.Contains("butt"))
            return MuscleGroup.Hips;
        if (name.Contains("lower_waist")
            || name.Contains("abdomen_y"))
            return MuscleGroup.Spine;
        if (name.Contains("thigh")
            || name.Contains("hip"))
            return MuscleGroup.LegUpper;
        if (name.Contains("shin"))
            return MuscleGroup.LegLower;
        if (name.Contains("right_right_foot")
            || name.Contains("left_left_foot")
            || name.Contains("ankle_x"))
            return MuscleGroup.Foot;
        if (name.Contains("upper_arm"))
            return MuscleGroup.ArmUpper;
		if (name.Contains("larm"))
            return MuscleGroup.ArmLower;
		if (name.Contains("hand"))
            return MuscleGroup.Hand;

        return MuscleGroup.None;
    }
}