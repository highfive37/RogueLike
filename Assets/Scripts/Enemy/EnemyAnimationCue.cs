// Which animation state an attack pattern's Execute() corresponds to - lets EnemyAttackState
// trigger the right clip (Assets/Editor-authored AnimatorControllers name their states "Attack"/
// "Shoot" to match) without switching on concrete IAttackPattern types.
public enum EnemyAnimationCue
{
    Attack,
    Shoot
}
