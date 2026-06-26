## ToDo List for Alpha Release

- 3d models !!! [done]
- correct material/outline for blueprints/previews
- correct driving across splines

- Better keyboard shortcuts !
- Vehicle controls !!!
    - Select fastest/cheapest route to valid structures
    - Load/Unload trucks [done]

- Check money before buying !!! [done]
    - Disable submit button when blueprint is empty or price is too high [schlecht]
- Hide blueprinted elements !!!
- Choose route on hover + hot key

- Pins [done]
    - Clickable !!! [done]
    - Scaling
    - Vehiclepins
        - Time
        - Correct position
        - Zitter nicht
        - Correct icon !!! [done]
    - Roadpins !!!
        - Cost
        - Duration
    - Consumerpins !!! [done]
        - Good
        - Reward
    - Producerpins

- Textures
- Outline scaling
- Canal tile black line artefact

- Better pathfinding for roads/canals
- Show canal owner 
- Compass

- Fix bug: Construction controls are disabled when hidden [done]
- Fix bug: Consumer/Producer spawn on canals [done]

Visuals/3D:
- Hide elements in blueprint
- Introduce scale factor based on tile size
- Visualize canal owner
- Dynamically make independent objects hoverable with predicate (edges, vehicles, structures)
- Display good on trucks
- More detailed/textured road
- Better outline textures
- Better player colors
- Biomes

UI:
- Outline pin when hovered/selected
- Better looking pins
- Compass
- Time
- Smooth camera movement when focusing object [done]
- Change keyboard shortcuts
- Audio

Logic:
- Highway
- Speedy canals [done]
- Generate public roads at start
- Destination queue
- Find fastest affordable road

Balancing:
- Adjust prices for constructible objects
    - Increasing prices for consequtive elements [done]
- Tolls (ports, roads, canals)
- Consumer request payout increases more linear
- Higher payout for foreign goods
- Change market cap calculation
- More balanced spawn
- Pick consumer for request at random (not from ready list)
- Change progress calculation