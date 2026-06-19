using System.Globalization;

namespace AssetStudio.Maui;

internal static class MeshViewerHtml
{
    // Mesh/shader data used to be pushed into an already-loaded page via repeated
    // EvaluateJavaScriptAsync calls. That turned out to be unreliable on WKWebView for
    // multi-megabyte payloads: a single huge call silently never executed, and splitting
    // it into many smaller sequential calls executed out of order, corrupting the
    // reassembled JSON. Loading a page is the one thing a WebView is built to handle
    // reliably at any size, so the data is now baked directly into the HTML as a JS
    // object/array literal and the whole page is freshly loaded each time a new
    // mesh/shader is selected.
    private const string SceneScript = """
    const scene = new THREE.Scene();
    scene.background = new THREE.Color(0x2b2b2b);
    const camera = new THREE.PerspectiveCamera(50, window.innerWidth / window.innerHeight, 0.01, 10000);
    camera.position.set(2, 2, 2);

    const renderer = new THREE.WebGLRenderer({ antialias: true });
    renderer.setSize(window.innerWidth, window.innerHeight);
    document.body.appendChild(renderer.domElement);

    const controls = new THREE.OrbitControls(camera, renderer.domElement);
    controls.enableDamping = true;

    scene.add(new THREE.AmbientLight(0xffffff, 0.6));
    const dirLight = new THREE.DirectionalLight(0xffffff, 0.9);
    dirLight.position.set(5, 10, 7);
    scene.add(dirLight);
    const dirLight2 = new THREE.DirectionalLight(0xffffff, 0.4);
    dirLight2.position.set(-5, -3, -7);
    scene.add(dirLight2);

    let currentMesh = null;

    function frameObject(geometry) {
        geometry.computeBoundingSphere();
        const sphere = geometry.boundingSphere;
        if (sphere && sphere.radius > 0) {
            controls.target.copy(sphere.center);
            const dist = sphere.radius * 2.5;
            camera.position.copy(sphere.center).add(new THREE.Vector3(dist, dist * 0.7, dist));
            camera.near = Math.max(sphere.radius / 100, 0.001);
            camera.far = sphere.radius * 100;
            camera.updateProjectionMatrix();
        }
        controls.update();
    }

    function clearCurrentMesh() {
        if (currentMesh) {
            scene.remove(currentMesh);
            currentMesh.geometry.dispose();
            currentMesh.material.dispose();
            currentMesh = null;
        }
    }

    function loadMesh(data) {
        clearCurrentMesh();
        if (!data.vertices || data.vertices.length === 0) {
            renderer.render(scene, camera);
            return;
        }

        const geometry = new THREE.BufferGeometry();
        geometry.setAttribute('position', new THREE.Float32BufferAttribute(data.vertices, 3));
        if (data.normals && data.normals.length > 0) {
            geometry.setAttribute('normal', new THREE.Float32BufferAttribute(data.normals, 3));
        }
        if (data.uvs && data.uvs.length > 0) {
            geometry.setAttribute('uv', new THREE.Float32BufferAttribute(data.uvs, 2));
        }
        if (data.indices && data.indices.length > 0) {
            geometry.setIndex(data.indices);
        }
        if (!data.normals || data.normals.length === 0) {
            geometry.computeVertexNormals();
        }

        const material = new THREE.MeshStandardMaterial({
            color: 0x9bb6ff,
            side: THREE.DoubleSide,
            metalness: 0.1,
            roughness: 0.85
        });
        currentMesh = new THREE.Mesh(geometry, material);
        scene.add(currentMesh);
        frameObject(geometry);
    }

    // Generic placeholder preview for Shader assets: we can't run the actual compiled
    // shader bytecode in WebGL, so this shows a sphere with a standard material whose
    // color/metalness/roughness are derived from the shader's own declared default
    // property values (or a deterministic hash of its name as a fallback), so different
    // shaders are at least visually distinguishable - not a faithful render of the real shader.
    function loadShaderPreview(r, g, b, metalness, roughness) {
        clearCurrentMesh();
        const geometry = new THREE.SphereGeometry(1, 48, 32);
        const material = new THREE.MeshStandardMaterial({
            color: new THREE.Color(r, g, b),
            metalness: metalness,
            roughness: roughness
        });
        currentMesh = new THREE.Mesh(geometry, material);
        scene.add(currentMesh);
        frameObject(geometry);
    }

    window.addEventListener('resize', () => {
        camera.aspect = window.innerWidth / window.innerHeight;
        camera.updateProjectionMatrix();
        renderer.setSize(window.innerWidth, window.innerHeight);
    });

    function animate() {
        requestAnimationFrame(animate);
        controls.update();
        renderer.render(scene, camera);
    }
    animate();
    """;

    public static string BuildMeshHtml(string threeJs, string orbitControlsJs, string geometryJson)
    {
        return BuildHtml(threeJs, orbitControlsJs, $"loadMesh({geometryJson});");
    }

    public static string BuildShaderHtml(string threeJs, string orbitControlsJs, float r, float g, float b, float metalness, float roughness)
    {
        var call = string.Format(CultureInfo.InvariantCulture, "loadShaderPreview({0},{1},{2},{3},{4});", r, g, b, metalness, roughness);
        return BuildHtml(threeJs, orbitControlsJs, call);
    }

    private const string HtmlShell = """
    <!DOCTYPE html>
    <html>
    <head>
    <meta charset="utf-8">
    <style>
      html, body { margin:0; padding:0; overflow:hidden; background:#2b2b2b; }
      canvas { display:block; }
    </style>
    </head>
    <body>
    <script>__THREEJS__</script>
    <script>__ORBITJS__</script>
    <script>
    __SCENE__
    __INIT__
    </script>
    </body>
    </html>
    """;

    private static string BuildHtml(string threeJs, string orbitControlsJs, string initCall)
    {
        return HtmlShell
            .Replace("__THREEJS__", threeJs)
            .Replace("__ORBITJS__", orbitControlsJs)
            .Replace("__SCENE__", SceneScript)
            .Replace("__INIT__", initCall);
    }
}
