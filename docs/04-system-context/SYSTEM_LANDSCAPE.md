# SYSTEM LANDSCAPE

This document maps likely system boundaries. It is not a commitment to implement everything.

## Layer 1 — Company Core
Shared by most future business divisions.

Potential systems:
- finance
- capital allocation
- company reputation
- brand
- executive strategy
- infrastructure
- organizational capacity
- staff
- legal / contracts
- analytics
- relationships
- risk
- company objectives

## Layer 2 — Market & World
External environment.

Potential systems:
- competitors
- labor / talent markets
- sponsor market
- audience
- economic conditions
- trends
- regulation
- technology
- opportunities
- crises

## Layer 3 — Business Divisions
Domain-specific operating units.

Possible future examples:

### Esports
- roster
- coaches
- competitions
- training
- esports-specific sponsors
- performance

### Traditional Sports
- athletes
- coaches
- leagues
- facilities
- sport-specific performance

### Media
- content
- audience
- platforms
- production capacity
- media rights

### Advertising
- campaigns
- clients
- brand inventory
- audience segments
- creative capacity

### Events
- venues
- ticketing
- scheduling
- sponsors
- attendance
- production

## Layer 4 — People
People can connect multiple systems.

Possible concepts:
- skill
- role
- salary
- contract
- personality
- morale
- loyalty
- ambition
- relationships
- fatigue
- development
- reputation

Do not implement all attributes by default.

## Layer 5 — Decision & Event Layer
Transforms simulation state into situations the player must reason about.

Sources:
- thresholds
- conflicts
- opportunities
- external changes
- AI competitor actions
- relationship changes
- resource pressure
- strategic consequences

## Design Rule
New business divisions should preferably:
1. reuse Company Core systems,
2. introduce a limited number of domain-specific systems,
3. create new strategic interactions with existing divisions.

Avoid creating each division as a separate mini-game with its own disconnected economy.
