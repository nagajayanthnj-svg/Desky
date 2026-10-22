# Unity project goes here

Created in task **DB-003** (spike S-0) as `unity/DeskButler`. Pin the Unity and Meta XR SDK versions in the root README once S-0 is done.

`unity/DeskButler/Packages/manifest.json` must contain:

```json
"dependencies": {
  "com.deskbutler.core": "file:../../../packages/com.deskbutler.core"
},
"testables": ["com.deskbutler.core"]
```

Commit the `.meta` files Unity generates inside `packages/com.deskbutler.core` on first import.
