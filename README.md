# CorpseBloomPlusPlus
CorpseBloomPlusPlus modifies the functionality of the Corpse bloom in some key areas in order to make the item much more effective, rewarding, situational, while still maintaining scaling balance.
The end result is an item that benefits those who do not have sustained leech capabilities, making combinations of rarely sought items useful once again, as well as having some key benefits which other items do not possess.
I hope that this change will be well received as it targets some core parts of gameplay & items which have been long overlooked by many in the community due to superior options elsewhere; however this mechanic should now provide a uniquely beneficial component to regenerative healing effects.

\*NEW\* CorpseBloomPlusPlus now has a Reserve HealthBar!

https://streamable.com/2bd7t

## Per default: 

"The CorpseBloom is a Lunar item introduced in Risk of Rain 2.

Like all Lunar items, the CorpseBloom has a powerful effect and a drawback. It increases all healing by 100%, but limits how much the user can heal at once to 10% max health. Each bloom increases this amount by 100%, and reduces the maximum healing per second by 50%.

Healing is "stored", so if the user is healed to maximum life, and then take damage, healing received earlier may still heal them."

## Per CorpseBloomPlusPlus:

* CorpseBlooms increase maximum healing per second by 10% per stack, does NOT increase all healing by 100%.
* CorpseBlooms decrease Healing Reserves by TotalReserves / CorpseBloom#.
* CorpseBlooms Reserve HP can be increased by 100% \* Rejuvenation Rack#/
* CorpseBlooms do NOT heal when player is full HP. Healing is instead stored in reserve.
* CorpseBlooms are now affected by all health regeneration effects.
* CorpseBlooms do not provide any regenerative effects by themselves, they only scale existing sources of healing (not actually a change, just a clarification).
* CorpseBlooms will now load a ReserveHealthBar underneath the existing HealthBar, which displays the healing % in reserve. Works in single & multiplayer.

Example: 2 Rejuvination racks provide 200% extra reserveHealth for a total of 300%. picking up 3 Corpsebloom reduced this back to 100% of Health, however the 3 Corpsebloom now apply 30% of health as healing per second, resulting in consuming the entire reserve to heal from 0->full health in 3.3seconds.

All players in the server will be affected by CorpseBloomPlusPlus should they take a CorpseBloom; but only those with the mod will see the UI.

## Installation:

Requires Bepinex, R2API, MiniRPCLib.

Place inside of Risk of Rain 2/Bepinex/Plugins/

## Upcoming Features:

v1.0.5 - Spectate other players HealthBar?

v1.0.5 - Fix UI "randomly" not loading on initialization.

## Changelog:
v1.0.4 - Added example video to readme

v1.0.3 - Fixed NullRef on player death.

v1.0.2 - UI HealthReserveBar implemented with net code.

v1.0.1 - Released.

## Issues:

Cannot view other players reserve bar while dead/spectating.
When HealthBar is initialized for the first time, sometimes does not display. Progress to next map for fix.

## Credits:

A huge thanks to iDeathHD for his time & persistence in helping me to understand various structures & operations when working with IL. I'm sure I was incredibly frustrating to teach.

Similarly, thanks to 0x0ade for his experience and quickly pointing logical flaws & the correct way to accomplish certain instructions.

Thanks to KubeRoot for identifying mistakes & encouraging me to delve into IL.

Thanks to Wildbook's MiniRPCLib for making the net code so incredibly convenient to implement!

Credit to CoiL#0518 for the Icon artwork!

All of you are greatly appreciated, along with everyone else in #development.