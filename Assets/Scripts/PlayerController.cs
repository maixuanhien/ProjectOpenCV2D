using System.Collections;
using System.Collections.Generic;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.IO;
using System.Drawing;
using Emgu.CV.Util;
using UnityEngine;
using System;

public class PlayerController : MonoBehaviour {

    private VideoCapture webcam;
    private Mat webcamFrame;
    private Mat webcamFrameGray;

    private CascadeClassifier cascadeClassifier;
    private string path = "D:\\Gamagora\\Interface\\ProjectOpenCV2D\\data\\lbpcascades\\lbpcascade_frontalface_improved.xml";

    private Rectangle[] faces;

    [SerializeField]
    private float speed;
    [SerializeField]
    private float jumpHeight;

    Rigidbody2D playerBody;
    Animator playerAnim;

    private int horizontal;
    private int vertical;

    private bool facingRight;
    private bool grounded;

    private void Start() {
        webcam = new VideoCapture(0);
        webcam.ImageGrabbed += new EventHandler(handleWebcamQueryFrame);
        webcamFrame = new Mat();
        webcamFrameGray = new Mat();
        cascadeClassifier = new CascadeClassifier(path);

        playerBody = GetComponent<Rigidbody2D>();
        playerAnim = GetComponent<Animator>();

        horizontal = 0;
        vertical = 0;

        facingRight = true;
    }

    private void Update() {
        if (webcam.IsOpened) {
            webcam.Grab();
        }
        if (webcamFrame.IsEmpty) {
            return;
        }
    }

    private void FixedUpdate() {
        playerBody.velocity = new Vector2(horizontal * speed, playerBody.velocity.y);

        if (horizontal > 0 && !facingRight) {
            flip();
        } else if (horizontal < 0 && facingRight) {
            flip();
        }

        if (horizontal != 0) {
            playerAnim.SetBool("run", true);
        } else {
            playerAnim.SetBool("run", false);
        }

        if (vertical > 0) {
            if (grounded) {
                grounded = false;
                playerBody.velocity = new Vector2(playerBody.velocity.x, jumpHeight);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        if(collision.gameObject.tag == "Ground") {
            grounded = true;
        }
    }

    private void flip() {
        facingRight = !facingRight;
        Vector3 theScale = transform.localScale;
        theScale.x = theScale.x * (-1);
        transform.localScale = theScale;
    }

    private void OnDestroy() {
        webcam.Dispose();
        CvInvoke.DestroyAllWindows();
    }

    private void handleWebcamQueryFrame(object sender, EventArgs e) {
        webcam.Retrieve(webcamFrame);

        Image<Hsv, byte> imageHSV = webcamFrame.ToImage<Hsv, byte>();
        CvInvoke.CvtColor(imageHSV, imageHSV, ColorConversion.Bgr2Hsv);

        double hValueMin = 70;
        double hValueMax = 100;

        double sValueMin = 100;
        double sValueMax = 255;

        double vValueMin = 100;
        double vValueMax = 255;

        Hsv lower = new Hsv(hValueMin, sValueMin, vValueMin);
        Hsv upper = new Hsv(hValueMax, sValueMax, vValueMax);

        Mat imgGray = imageHSV.InRange(lower, upper).Mat;

        VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
        VectorOfPoint biggestContour = new VectorOfPoint();
        int biggestContourIndex = -1;
        float biggestContourArea = 0f;

        Mat hierarchy = new Mat();
        CvInvoke.FindContours(imgGray, contours, hierarchy, RetrType.List, ChainApproxMethod.ChainApproxNone);

        for (int i = 0; i < contours.Size; i++) {
            if (CvInvoke.ContourArea(contours[i]) > biggestContourArea) {
                biggestContour = contours[i];
                biggestContourIndex = i;
                biggestContourArea = (float)CvInvoke.ContourArea(contours[i]);
            }
        }

        if (biggestContourIndex > -1) {
            CvInvoke.DrawContours(webcamFrame, contours, biggestContourIndex, new MCvScalar(255, 0, 0));
        }

        var moments = CvInvoke.Moments(biggestContour);
        int cx = (int)(moments.M10 / moments.M00);
        int cy = (int)(moments.M01 / moments.M00);
        Point p = new Point(cx, cy);
        int width = webcamFrame.Width;
        int height = webcamFrame.Height;

        if (cx < (float)width / 4) {
            horizontal = 1;
        } else if (cx > 3 * (float)width / 4) {
            horizontal = -1;
        } else {
            horizontal = 0;
        }
        if (cy < (float)height / 4) {
            vertical = 1;
        } else if (cy > 3 * (float)height / 4) {
            vertical = -1;
        } else {
            vertical = 0;
        }

        Mat flippedImage = webcamFrame.Clone();
        CvInvoke.Flip(webcamFrame, flippedImage, FlipType.Horizontal);
        CvInvoke.Imshow("Test", flippedImage);
    }
}
