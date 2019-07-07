# CorpseBloomPlusPlus
CorpseBloomPlusPlus modifies the functionality of the Corpsebloom in some key areas in order to make the item much more effective, rewarding, and situational while still maintaining scaling balance.
The end result is an item that benefits those who do not have sustained leech capabilities, making combinations of rarely sought items useful once again, as well as having some key benefits which other items do not possess.
I hope that this change will be well received as it targets some core parts of gameplay & items which have been long overlooked by many in the community due to superior options elsewhere; however this mechanic should now provide a uniquely beneficial component to regenerative healing effects.

\*NEW\* CorpseBloomPlusPlus now has a Reserve HealthBar! Compatable with SchoredAcre's update!

https://streamable.com/2bd7t

## Per default: 

"The CorpseBloom is a Lunar item introduced in Risk of Rain 2.

Like all Lunar items, the CorpseBloom has a powerful effect and a drawback. It increases all healing by 100%, but limits how much the user can heal at once to 10% max health. Each bloom increases this amount by 100%, and reduces the maximum healing per second by 50%.

Healing is "stored", so if the user is healed to maximum life, and then take damage, healing received earlier may still heal them."

## Per CorpseBloomPlusPlus:

* CorpseBlooms privide 100% of Health as a reserve, and can be increased by 100% per Rejuvination rack. 
* CorpseBlooms consume from reserve at a rate of 10% of maxHealth/s per CorpseBloom, can only restore health from stored reserves, does NOT increase all healing by 100%.
* CorpseBlooms decrease Healing Reserves by TotalReserves / CorpseBlooms.
* CorpseBlooms do NOT heal when player is full HP. Healing is instead stored in reserve.
* CorpseBlooms are now affected by all health regeneration effects (passive+active).
* CorpseBlooms will now load a ReserveHealthBar underneath the existing HealthBar, which displays the healing % in reserve. Works in single & multiplayer.
* CorpseBlooms do not provide any regenerative effects by themselves, they only scale existing sources of healing (not actually a change, just a clarification).

Example: 2 Rejuvination racks provide 200% extra reserveHealth for a total of 300%. picking up 3 Corpsebloom reduced this back to 100% of Health, however the 3 Corpsebloom now apply 30% of health as healing per second, resulting in consuming the entire reserve to heal from 0->full health in 3.3seconds.

All players in the server will be affected by CorpseBloomPlusPlus should they take a CorpseBloom; but only those with the mod will see the UI.

## Installation:

Requires Bepinex, R2API, MiniRPCLib.

Place inside of Risk of Rain 2/Bepinex/Plugins/

## Upcoming Features:

v1.0.6 - Spectate other players HealthBar?

## Changelog:
v1.0.6 - Resolved issue which prevented regenerative healing without corpsebloom, and resolved incorrect regen issue.

v1.0.5 - 

* Fixed balance issue with multiplicative heal scaling: removed corpseblooms "increase healing by 100%".

* Fixed issue with reserveHealth not incrementing upon rejuvinationRack pickup.

* Fixed issue with double double healing (yes you read that right. Racks double healing when added to reserve, and then double when consumed from reserve to heal).

* Fixed scaling with large quantities of racks and blooms.

* Fixed various broken hooks & ensured compatability with Scorched Acres update.

* Fixed UI "randomly" not loading on initialization.

v1.0.4 - Added example video to readme

v1.0.3 - Fixed NullRef on player death.

v1.0.2 - UI HealthReserveBar implemented with net code.

v1.0.1 - Released.

## Issues:

Cannot view other players reserve bar while dead/spectating.

## Credits:

A huge thanks to iDeathHD for his time & persistence in helping me to understand various structures & operations when working with IL. I'm sure I was incredibly frustrating to teach.

Similarly, thanks to 0x0ade for his experience and quickly pointing logical flaws & the correct way to accomplish certain instructions.

Thanks to KubeRoot for identifying mistakes & encouraging me to delve into IL.

Thanks to Wildbook's MiniRPCLib for making the net code so incredibly convenient to implement!

Credit to CoiL#0518 for the Icon artwork!

All of you are greatly appreciated, along with everyone else in #development.