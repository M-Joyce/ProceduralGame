using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    public CharacterController controller;
    public float speed = 6f;
    public float turnSmoothTime = 0.1f;
    float turnSmoothVelocity;
    public Transform cam;

    // Start is called before the first frame update


    // Update is called once per frame
    void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal"); //x
        float vertical = Input.GetAxisRaw("Vertical"); //z
        

        if (Input.GetKey(KeyCode.Q))
        {
            controller.Move(Vector3.up * speed * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.E))
        {
            controller.Move(Vector3.down * speed * Time.deltaTime);
        }



        Vector3 direction = new Vector3(horizontal, 0, vertical).normalized; //normalized so holding 2 keys doesnt make you faster

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y; //rotate player in direction of movement
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime); //smooth rotation
            transform.rotation = Quaternion.Euler(0f,angle,0f);

            Vector3 moveDir = Quaternion.Euler(0f,targetAngle,0f) * Vector3.forward;
            controller.Move(moveDir.normalized * speed * Time.deltaTime);
        }
    }
}
