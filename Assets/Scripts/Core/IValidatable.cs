using System.Collections.Generic;

// Implemented by ScriptableObject data assets that can be misconfigured in ways Unity's own
// Inspector won't flag (a null prefab reference, a negative cooldown, an empty pattern array).
// Validate() is pure - no UnityEditor dependency - so the same check runs from OnValidate() for
// immediate per-asset Inspector feedback AND from the project-wide Editor menu scan
// (Assets/Editor/DataValidationMenu.cs) without duplicating logic, and can be unit-tested like
// any other pure logic in this codebase.
public interface IValidatable
{
    // Appends one human-readable message per problem found. Does not clear errors first, so a
    // derived type's override can call base.Validate(errors) first and keep accumulating.
    void Validate(List<string> errors);
}
