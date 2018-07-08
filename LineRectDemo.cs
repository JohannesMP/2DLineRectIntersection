using UnityEngine;
using UnityEditor;
using Utils;

// For in-editor testing only, so place in an 'Editor' folder
public class LineRectDemo : MonoBehaviour
{
    public GameObject obj1;
    public Vector3 Pos1 { get { return obj1.transform.position; } }

    public GameObject obj2;
    public Vector3 Pos2 { get { return obj2.transform.position; } }

    public Rect Rect2D
    {
        get
        {
            var width = transform.localScale.x;
            var height = transform.localScale.y;
            var xmin = transform.position.x - width / 2;
            var ymin = transform.position.y - height / 2;
            return new Rect(xmin, ymin, width, height);
        }
    }

    private void OnDrawGizmos()
    {
        if (obj1 == null || obj2 == null)
        {
            Debug.LogWarning("Obj1 and obj2 need to be set");
            return;
        }

        Gizmos.color = new Color(1, 0.5f, 0);
        DrawRect(Rect2D);

        Raycast2DLineRect.LineRectResult result = new Raycast2DLineRect.LineRectResult();
        Raycast2DLineRect.RaycastLineRect(Pos1, Pos2, Rect2D, ref result);

        Gizmos.color = result.HaveHit ? Color.red : Color.white;
        Gizmos.DrawLine(Pos1, Pos2);

        if (result.HaveHit)
        {
            Vector3 dir = Pos2 - Pos1;
            Vector3 entry = Pos1 + dir * result.t_entry;
            Vector3 exit  = Pos1 + dir * result.t_exit;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(entry, exit);

            Handles.Label(entry, result.t_entry.ToString());
            Handles.Label(exit, result.t_exit.ToString());
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Pos1, 0.05f);
        Gizmos.DrawWireSphere(Pos2, 0.05f);
    }

    static void DrawRect(Rect rect)
    {
        Vector3 topLeft = new Vector3(rect.xMin, rect.yMax, 0);
        Vector3 bottomRight = new Vector3(rect.xMax, rect.yMin, 0);

        Gizmos.DrawLine(rect.min, topLeft);
        Gizmos.DrawLine(rect.min, bottomRight);
        Gizmos.DrawLine(rect.max, topLeft);
        Gizmos.DrawLine(rect.max, bottomRight);
    }
}