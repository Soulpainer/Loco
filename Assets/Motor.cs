using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class Motor : MonoBehaviour
{
    [SerializeField] private SplineContainer _spline;
    [SerializeField] private float _speed;
    
    // Update is called once per frame
    void Update()
    {
        Tick(_spline.Spline, transform.forward*_speed*Time.deltaTime);
    }

    public Vector3 Tick(Spline spline, Vector3 vector, bool normalized=false){

        float3 nextPoint;
        //float3 curPoint;
        float time;

        Vector3 next = transform.position+vector;    
        SplineUtility.GetNearestPoint(spline, new float3(next), out nextPoint, out time);
        Vector3 nextVec = new Vector3(nextPoint.x, nextPoint.y, nextPoint.z);


        //SplineUtility.GetNearestPoint(spline, new float3(transform.position), out curPoint, out time);
        //Vector3 curVec = new Vector3(curPoint.x, curPoint.y, curPoint.z);

        // Vector3 pos = vec;// + _spline.transform.position;
        // Debug.DrawLine(pos+Vector3.left, pos-Vector3.left);
        // Debug.DrawLine(pos+Vector3.forward, pos-Vector3.forward);

        // Debug.Log(point);

        var gravity = Vector3.zero;

        var dir = (new Vector3(nextPoint.x, nextPoint.y, nextPoint.z) - transform.position).normalized;
        var notOnRail=false;
        if(time <=0.01 || time>=0.99){
            dir = Vector3.zero;    
            gravity = Vector3.down*0.04f;
            notOnRail = true;            
        }

        if(normalized){
            
            if(notOnRail){
                // Debug.Log("NOTONRAIL");
                // vector.y*=vector.y;
                // return Vector3.zero;//.normalized;
               return gravity;
                
            } 
            var grav = transform.position.y-nextPoint.y;
            return new Vector3(nextPoint.x, nextPoint.y, nextPoint.z) - transform.position+Vector3.down*grav;
        }
        
        // if(Vector3.Angle(dir, vector.normalized)<90){
        //     //transform.rotation = Quaternion.LookRotation(dir);
        //     return transform.position +  dir * vector.magnitude;
        // }

        //return transform.position +  dir * vector.magnitude;
        //(transform.position.y>=curVec.y)?Vector3.down*9.8f:Vector3.zero;
        //dir.y=0;

        if(notOnRail){
            return vector+gravity;
        }
        return dir * vector.magnitude+gravity;
        //return transform.position;
    }
}
