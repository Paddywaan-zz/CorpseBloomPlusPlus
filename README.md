# CorpseBloomPlusPlus
CorpseBloomPlusPlus modifies the functionality of the Corpse bloom in some key areas in order to make the item much more effective, rewarding, situational, while still maintaining scaling balance.
The end result is an item that benefits those who do not have sustained leech capabilities, making combinations of rarely sought items useful once again, as well as having some key benefits to which other items do not posses.
To this end, many synergies and item combinations are a possible effective use with this modification of CorpseBloom, allowing players to effectively distribute more "useless" loot to a playstyle in which it becomes an effective core mechanic of gameplay.

\*NEW\* CorpseBloomPlusPlus now has a Reserve Healthbar!

## Per default: 

"The CorpseBloom is a Lunar item introduced in Risk of Rain 2.

Like all Lunar items, the CorpseBloom has a powerful effect and a drawback. It increases all healing by 100%, but limits how much the user can heal at once to 10% max health. Each bloom increases this amount by 100%, and reduces the maximum healing per second by 50%.

Healing is "stored", so if the user is healed to maximum life, and then take damage, healing received earlier may still heal them."

## Per CorpseBloomPlusPlus:

* CorpseBlooms increase Healing by 100% per stack, and increase maximum healing per second by 10% per stack.
* CorpseBlooms decrease Healing Reserves by TotalReserves(100% HP with one CorpseBloom) / Stack#.
* CorpseBlooms Reserve HP can be increased by 100%*Rejuvenation Rack#
* CorpseBlooms do NOT heal when player is full HP. Healing is instead stored in reserve.
* CorpseBlooms are now affected by all health regeneration effects.
* CorpseBlooms do not provide any regenerative effects by themselves, they only scale existing sources of healing (not actually a change, just a clarification).
* Picking up a CorpseBloom will now load a new HealthBar undernearth the existing one, which displays the % & max capacity in reserve. Works in single & multiplayer.

I hope that this change will be well received as it targets some core parts of gameplay & items which have been long overlooked by many in the community due to superior options elsewhere; however this mechanic should now provide a uniquely beneficial component to regenerative effects which do not currently exist in a useful manner within the game.


## Installation:

Requires Bepinex, R2API, MiniRPCLib.

Place inside of Risk of Rain 2/Bepinex/Plugins/

## Upcoming Features:

v1.0.3 - Spectate other players healthbar?

## Changelog:

v1.0.2 - UI HealthReserveBar implemented with netcode.

v1.0.1 - Released

## Issues:

Cannot view other players reserveBar while dead/spectating.

## Credits:

A huge thanks to iDeathHD for his time & persistence in helping me to understand various structures & operations when working with IL. I'm sure I was incredibly frustrating to teach.

Similarly, thanks to 0x0ade for his boundless knowledge and quickly pointing logical flaws & the correct way to accoumplish certain instructions.

Thanks to KubeRoot for identifying mistakes & encouraging me to delve into IL.

Thanks to Wildbook's MiniRPCLib for making the netcode so incredibly convenient to implement!

All of you are greatly apreciated, along with everyone else in #development.
