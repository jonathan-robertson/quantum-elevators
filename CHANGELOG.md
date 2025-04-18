# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [5.0.1] - 2025-04-11

- rebuilt mod for 7dtd 1.4 (b8)
- update pipeline to build multiple versions

## [5.0.0] - 2025-02-15

- update references for 7dtd-1.3-b9

## [4.0.0] - 2024-12-10

- update references for 7dtd-1.2-b27

## [3.1.0] - 2024-10-02

- improve debug logging and fix some incorrect logs
- rebuild for 1.1-b14 stable

## [3.0.2] - 2024-07-20

- update references for 7dtd-1.0-b326

## [3.0.1] - 2024-07-12

- update references for 7dtd-1.0-b316

## [3.0.0] - 2024-06-28

- fix particles to no longer surround weapon
- override default 'container locked' text
- remove journal tips (discontinued in 1.0)
- remove sign action from secure panel
- update references for 7dtd-1.0

## [2.0.2] - 2023-08-01

- update support link & username in readme & logs

## [2.0.1] - 2023-06-30

- update to support a21 b324 (stable)

## [2.0.0] - 2023-06-25

- add `electricTier2` trader stage requirement
- add journal entry on login
- add key bindings to instructions
- add recipe unlock to electronics magazine
- clarify/correct note about removing this mod
- clean large rotation values
- limit jump height to 1 block when on panel
- no longer stop jumping; needed for `onGround` check
- update console command for a21
- update panel height to 3 blocks for jump room
- update to a21 mod-info file format
- update to a21 references
- update to reference `onGround` vs `bJumping` (retired)

## [1.1.4] - 2023-05-13

- add dll version log message on awake
- fix travel through rotated multidim blocks
- update console enable/disable debug command
- update readme with setup steps

## [1.1.3] - 2023-04-16

- fix crash caused by complex multi-dim blocks
- fix unintentional offset check at wrong time
- update upper y check to 253 build limit

## [1.1.2] - 2023-03-05

- add conf check when applying warp effects buff
- fix error log interpolation
- fix error on warp from portable to locked secured
- improve debug logging around travel prevention
- remove unnecessary particle rotation reference

## [1.1.1] - 2023-02-24

- disable block decoration val; caused warp issues
- fix quantum elevators being broken on first launch

## [1.1.0] - 2023-02-24

- fix block id -1 bug
- fix push bounding box
- fix push warping all players bug
- update push console command for ease of use
- update push to no longer eject onto air

## [1.0.2] - 2023-02-23

- fix error with client-side mod firing in server

## [1.0.1] - 2023-02-23

- add video link to readme
- fix block localization text
- fix journal grammar mistake

## [1.0.0] - 2023-02-22

- add admin command to toggle debug logging
- add automated clearing of target panel before warp
- add buff showing elevation with control tips
- add support for local players
- allow elevator blocks to be stacked up to 5
- finalize readme and journal entries
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
