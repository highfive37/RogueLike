using System.Runtime.CompilerServices;

// Lets the test assemblies reach `internal` seams (e.g. GameFloors.ParseFloorJson,
// GameBalance.ParsePlayerBaseStats) without widening them to public on the game's API surface.
[assembly: InternalsVisibleTo("RogueLike.Tests.EditMode")]
[assembly: InternalsVisibleTo("RogueLike.Tests.PlayMode")]
