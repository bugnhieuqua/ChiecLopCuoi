using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("0 = Stands still on screen (moves with camera). 0.2 = Slow background. 1 = World ground (moves 100% relative to camera).")]
    public float parallaxSpeed = 0.5f;
    public bool isLooping = false;

    private Transform cam;
    private Vector3 startPos;
    private float spriteWidth;

    void Start()
    {
        if (Camera.main != null)
            cam = Camera.main.transform;
            
        startPos = transform.position;

        if (isLooping)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                spriteWidth = sr.bounds.size.x;
            }
            else
            {
                // If it's a Tilemap or compound object, compute total bounds
                var renderers = GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds b = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                    {
                        b.Encapsulate(renderers[i].bounds);
                    }
                    spriteWidth = b.size.x;
                }
            }
        }
    }

    void LateUpdate()
    {
        if (cam == null)
        {
            if (Camera.main != null)
                cam = Camera.main.transform;
            else
                return;
        }

        // Logic Parallax:
        // parallaxSpeed = 0.0 -> followFactor = 1.0 -> object follows camera 100% (static on screen, e.g. Sky)
        // parallaxSpeed = 0.2 -> followFactor = 0.8 -> object moves at 20% relative speed on screen (Mid mountains)
        // parallaxSpeed = 1.0 -> followFactor = 0.0 -> object static in world, scrolls 1:1 with camera drag (Near Ground)
        float followFactor = 1f - Mathf.Clamp01(parallaxSpeed);
        float distX = cam.position.x * followFactor;
        transform.position = new Vector3(startPos.x + distX, startPos.y, startPos.z);

        // Infinite loop handling if needed
        if (isLooping && spriteWidth > 0 && parallaxSpeed > 0)
        {
            float relativeDist = cam.position.x * parallaxSpeed;
            if (relativeDist > startPos.x + spriteWidth)
            {
                startPos.x += spriteWidth;
            }
            else if (relativeDist < startPos.x - spriteWidth)
            {
                startPos.x -= spriteWidth;
            }
        }
    }
}
