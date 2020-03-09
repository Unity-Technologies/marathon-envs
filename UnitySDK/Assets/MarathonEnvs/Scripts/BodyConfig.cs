using System;
using static BodyHelper002;

public class BodyConfig
{
	public Func<string, BodyPartGroup> GetBodyPartGroup;
	public Func<string, MuscleGroup> GetMuscleGroup;
	public Func<BodyPartGroup> GetRootBodyPart;
	public Func<MuscleGroup> GetRootMuscle;

}
