A "mess-around" project to learn some ECS. 
Has proof-of-concept systems for a Survivor-like type game.
Very unorganized, multiple systems and components in 1 file, so dont browse on Github, download the project and browse in your IDE.

POC's:
* ProjectileSkillSystem (has prefabs for shotguns, miniguns, assault rifles)
* GrenadeSystem (with Cluster grenades)
* Rockets
* Ricochet'ing bullets (Demonstrating collision callbacks)
* XP pickups (with jobified magnet)
* ScriptableObject -> WeaponData ECS components workflow
* Bridge to / from GameObjects world
* Generic VFX system that spawns and plays VFX Graph assets
* Generic spawning system