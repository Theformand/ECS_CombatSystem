


A "mess-around" project to learn some ECS. 
Has proof-of-concept systems for a Survivor-like type game.
Very unorganized, multiple systems and components in 1 file, so dont browse on Github, download the project and browse in your IDE.

YOU CAN USE THIS CODE FOR WHATEVER YOU WANT, BUT THERE WILL BE NO Q&A AND NO PULL REQUESTS. Fork at your leisure

POC's:
* ProjectileSkillSystem (has prefabs for shotguns, miniguns, assault rifles)
* GrenadeSystem (with Cluster grenades)
* Rockets
* Ricochet'ing bullets (Demonstrating collision callbacks)
* Knockback
* Convert AnimationCurve to ECS data
* XP pickups (with jobified magnet)
* ScriptableObject -> IComponentData workflow
* Bridge to / from GameObjects world
* Generic VFX system that spawns and plays VFX Graph assets
* Generic spawning system
* Generate custom mesh in Monobehaviour -> send to ECS and create collider from it


https://github.com/Theformand/ECS_CombatSystem/assets/9436242/3a7b95e3-c272-47fb-b6d0-b8799615cbec

