# Global Logistics

Global Logistics is an online RTS logistics simulator, that is still accessible to the average person. For a detailed description, please refer to the [Wiki](https://collab.dvb.bayern/spaces/TUMgameslab2026summer/pages/2718499622/Global+Logistics).

## Gameplay Video

<p align="center">
  <a href="https://collab.dvb.bayern/spaces/TUMgameslab2026summer/pages/2718499622/Global+Logistics?preview=/2718499622/2936898928/Global_Logistics_Gameplay.mp4">
    <img src="Docs/images/thumbnail.png" alt="Watch the Demo Video" width="600">
  </a>
</p>

## Technical Details

Many of the things in this game are procedurally generated. First we generate the spherical map based on an icosphere, then we generate the environment (oceans, continents, mountains and forests). The geometries of roads and canals are also generated procedurally to allow for smooth intersections. 

<div align="center">
  <table>
    <tr>
      <td align="center">
        <img src="Docs/images/map_geometry.gif" alt="Map geometry" width="300"><br>
        <p>Map geometry</p>
      </td>
      <td align="center">
        <img src="Docs/images/map.png" alt="Map" width="300"><br>
        <p>Resulting Map</p>
      </td>
    </tr>
  </table>
</div>

<div align="center">
  <table>
    <tr>
      <td align="center">
        <img src="Docs/images/roads.png" alt="roads" width="300"><br>
        <p>Procedural road geometry</p>
      </td>
      <td align="center">
        <img src="Docs/images/canals.png" alt="canals" width="300"><br>
        <p>Procedural canals</p>
      </td>
    </tr>
  </table>
</div>

<div align="center">
  <img src="Docs/images/projection.gif" alt="projection" width="500"><br>
  <p>Vertex shader projection, so that more of the spherical map is visible when zooming in.</p>
</div>


For more details refer to the respective pages in the wiki:
* [Technical Details](https://collab.dvb.bayern/spaces/TUMgameslab2026summer/pages/2718499670/Technical+Details)
* [Map Generation](https://collab.dvb.bayern/spaces/TUMgameslab2026summer/pages/2718499678/Map+Generation)
* [Geometry Generation](https://collab.dvb.bayern/spaces/TUMgameslab2026summer/pages/2718499686/Geometry+Generation)
* [Road and Canal Geometry Generation](https://collab.dvb.bayern/spaces/TUMgameslab2026summer/pages/2836859811/Road-+Canal+Geometry+Generation)