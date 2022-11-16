# Quantum Elevator

[![🧪 Tested On](https://img.shields.io/badge/🧪%20Tested%20On-A20.6%20b9-blue.svg)](https://7daystodie.com/) [![📦 Automated Release](https://github.com/jonathan-robertson/quantum-elevator/actions/workflows/release.yml/badge.svg)](https://github.com/jonathan-robertson/quantum-elevator/actions/workflows/release.yml)

- [Quantum Elevator](#quantum-elevator)
  - [Summary](#summary)
  - [Features](#features)
  - [TODO](#todo)

## Summary

7 Days to Die modlet: A server-side elevator mod.

## Features

- TODO

## TODO

```mermaid
flowchart LR
    nat(Natural State)
    charging(Charging)
    charged(Fully Charged)
    
    above[Teleport to Floor Above]
    below[Teleport to Floor Below]

    enter{Enter}
    wait{Wait}
    up{Look Up}
    down{Look Down}
    exit{Exit}

    enter-->charging
    exit-->nat
    subgraph disable[Cannot Use Weapons]
        enter
        charging-->wait-->charged
        charged-->up-->above-->charged
        charged-->down-->below-->charged
    end

    subgraph enable[Can Use Weapons]
        exit
        nat
    end

    nat-->enter
    charging-->exit
    charged-->exit
```
