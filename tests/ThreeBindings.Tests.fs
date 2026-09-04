/// <summary>Tests for the typed three.js scene graph bindings.</summary>
/// <remarks>
/// Geometry, materials, meshes, lights, and the scene graph need no GPU:
/// these tests run in Node. The WebGLRenderer stays out: it needs a canvas
/// and a GL context, so only the browser demo exercises it.
/// </remarks>
module ThreeBindings.Tests

open Mibo.Testing.QUnit
open Mibo.Fable.ThreeJS.Bindings

QUnit.``module`` "ThreeJS bindings"

QUnit.test(
  "scene graph wires typed handles",
  fun assert' ->
    let scene = Scene()
    use geometry = new BoxGeometry(1.0, 2.0, 3.0)
    use material = new MeshBasicMaterial({ color = 0xff0000 })
    let mesh = Mesh(geometry :> BufferGeometry, material :> Material)
    let light = AmbientLight(0xffffff, 0.9)

    mesh.position.set(1.0, 2.0, 3.0)
    mesh.rotation.set(0.5, 1.0, 0.0)
    scene.add(mesh :> Object3D)
    scene.add(light :> Object3D)
    scene.remove(mesh :> Object3D)
    scene.add(mesh :> Object3D)
    scene.clear()

    assert'.ok(true, "typed scene graph builds without errors")
)

QUnit.test(
  "lit material and directional light join the scene",
  fun assert' ->
    let scene = Scene()

    use material =
      new MeshStandardMaterial(
        {
          color = 0x00ff00
          roughness = 0.6
          metalness = 0.1
        }
      )

    use geometry = new BoxGeometry(1.4, 1.4, 1.4)
    let mesh = Mesh(geometry :> BufferGeometry, material :> Material)
    let dir = DirectionalLight(0xffffff, 1.2)
    let camera = PerspectiveCamera(75.0, 1.5, 0.1, 1000.0)

    dir.position.set(2.0, 3.0, 4.0)
    camera.position.set(0.0, 0.0, 5.0)
    camera.lookAt(0.0, 0.0, 0.0)
    scene.add(mesh :> Object3D)
    scene.add(dir :> Object3D)

    assert'.ok(true, "lit scene builds without errors")
)

QUnit.test(
  "texture loader constructs without a canvas",
  fun assert' ->
    let loader = TextureLoader()
    assert'.ok((loader :> obj) <> null, "loader constructs")
)
