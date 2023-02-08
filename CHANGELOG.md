# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Required TODOs

- [x] clean up Warp method so it receives all data that was already pulled from patch (save processing)
- [ ] prevent xray wall bumping (I did clip into a block when a zombie was on the pad)
  - player could be "bumped" into a wall if an entity is standing in the warp destination (exploitable!)
  - this could be another player or any other type of entity (such as zombie or animal)
  - ~~perhaps zombie should be despawned~~
  - if player is standing there, perhaps player attempting to warp should be blocked and notified...
  - or perhaps the one standing on the panel should be shifted off the panel (this could work for zombies and animals as well!)
- [ ] delete unused buffs, etc.
- [ ] double-check abs code to confirm permissions haven't broken (switched from internalId to userId)
- [x] ~~pass along player rotation when adjusting pitch after warp~~
- [x] ~~reset player pitch to neutral when initially stepping on panel?~~
  - ~~to prevent accidental warping~~

## Hopeful TODOs

- [x] ~~change text pop-up for secure panels to something better~~
- [x] ~~change activation effect (set/enter pass? or just error sound?)~~
- [x] ~~change options when holding E (remove sign piece if possible)~~
- [ ] revisit particle, screen effects, and sounds  

## [0.2.0] - ?

- add admin command to toggle debug logging
- add buff showing elevation with control tips
- add support for local players
- allow elevator blocks to be stacked up to 5
- prevent repeated warps from held key
- rename project
- unlock crouch on warp in local games
- update code to match csharp standards
- update to jump/crouch controls vs look angles
- wrap debug logging in disabled debug flag

## [0.1.0] - 2022-11-17

- add elevator 'look' controls
- add elevator recipes
- add journal entries
- add locks/passwords to elevator
- add notifications for locked elevator use
- add on-demand password chaining
- add panels to trader for purchase
- add particle effects when on elevator
- add quantum elevator block
- add support for portable elevator pad
