using System;
using System.Linq;
using System.Collections.Generic;

public enum SkillIdx
{
	a, b, c, d, e, f
}

public class SkillManager
{
	private static SkillManager _inst;
	public static SkillManager Instance => _inst ??= new();

	private readonly Dictionary<SkillIdx, int> levels = new()
	{
		{ SkillIdx.a, 0 },
		{ SkillIdx.b, 0 },
		{ SkillIdx.c, 0 },
		{ SkillIdx.d, 0 },
		{ SkillIdx.e, 0 },
		{ SkillIdx.f, 0 }
	};

	private const int 만렙 = 4;
	public void ShowSkillTree()
	{
		int count = 3; // 랜덤으로 표시할 스킬 개수
		foreach (var skill in levels.OrderBy(g => Guid.NewGuid()))
		{
			// 만렙 아닌 스킬만 표시
			if (skill.Value < 만렙 && count-- > 0) Console.WriteLine($"{skill.Key}: {skill.Value}");
		}
	}

	public void LearnSkill(SkillIdx skill)
	{
		if (levels[skill] < 만렙) levels[skill]++;
	}
}