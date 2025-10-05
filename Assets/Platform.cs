using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro.EditorUtilities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;

public class Platform : MonoBehaviour
{
    [SerializeField] private SplineContainer _spline;
    [SerializeField] Motor[] _motors;
    [SerializeField] float _speed = 10.0f;
    // Start is called before the first frame update
    private Vector3 _inertia = new Vector3();
    private float spd = 0;
    List<Vector3> _motorPositions = new List<Vector3>();
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 moveVec = _inertia;
        
        Vector3 motorsDirection = Vector3.zero;
        float gravityFactor = 1;

        if(_motorPositions.Count > 0){
            motorsDirection = _motorPositions[1]-_motorPositions[0];
            float motorDot = Vector3.Dot(motorsDirection, transform.forward);
            motorsDirection *= motorDot;
            gravityFactor = (_motorPositions[0]-_motorPositions[1]).y;
        }

        spd = _speed+gravityFactor*0.5f;


        _motorPositions = new List<Vector3>();
        foreach (var motor in _motors)
        {            
            //Vector3 pos = motor.Tick(_spline.Spline, motor.transform.forward*spd);
            //_motorPositions.Add(motor.transform.position+pos.normalized*0.1f);// +pos);
            //Vector3 pos = motor.Tick(_spline.Spline, (motor.transform.forward*spd).normalized*0.1f);
            Vector3 nextPos = motor.Tick(_spline.Spline, (motor.transform.forward*spd));
            Vector3 pos = motor.Tick(_spline.Spline, (motor.transform.forward*spd).normalized*0.1f);
            //var vvc = motor.transform.position+nextPos;
            // if(moveVec.y>nextPos.y){
            //     nextPos = nextPos-(moveVec*0.5f);
            // }
            var motorPos = motor.transform.position+nextPos;
            //motorPos.y = nextPos.y;//Mathf.Max(motorPos.y, nextPos.y);
            _motorPositions.Add(motorPos);// +pos);
            moveVec+=nextPos;
            Debug.DrawLine(motor.transform.position +pos+Vector3.left, motor.transform.position +pos-Vector3.left, Color.green);
            Debug.DrawLine(motor.transform.position +pos+Vector3.forward, motor.transform.position +pos-Vector3.forward, Color.green);
        }

        Debug.DrawLine(transform.position + (moveVec*10)+Vector3.left, transform.position + (moveVec*10)-Vector3.left, Color.red);
        Debug.DrawLine(transform.position + (moveVec*10)+Vector3.forward, transform.position + (moveVec*10)-Vector3.forward, Color.red);

        //var rot = Quaternion.LookRotation(moveVec);

        //Vector3 rvec = transform.InverseTransformPoint(transform.position + moveVec);//transform.InverseTransformPoint(moveVec);
        //rvec.z = math.abs(rvec.z);
        //Debug.Log(moveVec);



        Vector3 rvec = moveVec;

//    if(motorDot>0){
//        moveVec -= moveVec.normalized* (motorPositions[1]-motorPositions[0]).y*2;
//    }
//     if(motorDot<0){
//        moveVec -= moveVec.normalized* (motorPositions[0]-motorPositions[1]).y*2;
//    }
        //moveVec += (motorPositions[1]-motorPositions[0]);
        // // Debug.DrawLine(transform.position, transform.position + moveVec.normalized*5, Color.magenta);
        //         rvec = transform.InverseTransformPoint(transform.position + rvec);
        // //rvec.z=Mathf.Abs(rvec.z);
        // rvec = transform.TransformPoint(rvec)-transform.position;


        //Debug.DrawLine(transform.position, transform.position + rvec.normalized*5, Color.blue);
        //rvec.x = Mathf.Abs(rvec.x);
        //rvec.y = Mathf.Abs(rvec.y);
        //rvec.z = Mathf.Abs(rvec.z);
        
                if(rvec.magnitude>0){
            //transform.rotation = Quaternion.LookRotation(rvec);
        } else {
            rvec = transform.forward;
        }
        //transform.Translate(Vector3.forward*_speed*Time.deltaTime);
        transform.position += moveVec/_motors.Length*Time.deltaTime;
        // var yvc = transform.position;
        // yvc.y = Mathf.Max(transform.position.y, ((_motorPositions[1]-_motorPositions[0])*0.5f).y);
        // transform.position = yvc;


        //rvec = transform.InverseTransformVector(rvec.normalized);
        //rvec.z=Mathf.Abs(rvec.z);
        //rvec = transform.TransformVector(rvec);
 
        Debug.DrawLine(transform.position, transform.position + rvec*10, Color.magenta);
                       
        // var dot = Vector3.Dot(rvec, transform.forward);
        // rvec *= dot;
        // if(rvec!=Vector3.zero){// rvec.magnitude>0){
        //     transform.rotation = Quaternion.LookRotation(rvec.normalized);
        // }
        // if(_motors[0].transform.position.y>motorPositions[0].y){
        //     motorPositions[0] = new Vector3(motorPositions[0].x, -9.8f, motorPositions[0].z);
        // }
        // if(_motors[1].transform.position.y>motorPositions[1].y){
        //     motorPositions[1] = new Vector3(motorPositions[1].x, -9.8f, motorPositions[1].z);
        // }
        if(motorsDirection.magnitude>0.001f){//if(motorsDirection.magnitude>0.5f){
            transform.rotation = Quaternion.LookRotation(motorsDirection.normalized, Vector3.up);
        }

        _inertia = moveVec*0.98f;//+Vector3.down*9.8f;//*Time.deltaTime;//gravity


        List<Vector3> _motorsAfterPositions = new List<Vector3>();
        foreach (var motor in _motors)
        {            
            Vector3 pos = motor.Tick(_spline.Spline, Vector3.zero, true);

            var motorPos = motor.transform.position+pos;
            _motorsAfterPositions.Add(motorPos);
            Debug.DrawLine(motor.transform.position +pos+Vector3.left, motor.transform.position +pos-Vector3.left, Color.blue);
            Debug.DrawLine(motor.transform.position +pos+Vector3.forward, motor.transform.position +pos-Vector3.forward, Color.blue);
        }
        var yvc = transform.position;
        var mP = (_motorsAfterPositions[1]+_motorsAfterPositions[0])*0.5f;
        //yvc.y = Mathf.Max(transform.position.y, mP.y);
        yvc.y = mP.y;
        //yvc.x = mP.x;
        //yvc.z = mP.z;
        Debug.DrawLine(mP+Vector3.left, mP-Vector3.left, Color.yellow);
        Debug.DrawLine(mP+Vector3.forward, mP-Vector3.forward, Color.yellow);
        transform.position = yvc;
    }

    private Quaternion CloseLocalRotation(Quaternion rotation){
        Vector3 vec = rotation.eulerAngles;
        //vec = transform.InverseTransformDirection(vec);
        //vec.x = Mathf.Round((vec.x) / 180) * 180;
        //vec.y = Mathf.Round((vec.y) / 180) * 180;
        //vec.z = Mathf.Round((vec.z) / 180) * 180;
        //vec = transform.TransformDirection(vec);
        return Quaternion.Euler(vec);
    }
}

