
#include <opencv2/opencv.hpp>
#include <iostream>

cv::Mat convert_image_to_IA8(const cv::Mat& original) {
    int w = original.cols;
    int h = original.rows;
    cv::Mat converted_img = original.clone(); // Create a copy of the original image

    for (int y = 0; y < h; ++y) {
        for (int x = 0; x < w; ++x) {
            cv::Vec4b px = original.at<cv::Vec4b>(y, x); // Assuming original image has 4 channels (RGBA)

            // Using the RGB -> Digital Luma conversion here
            uchar intensity = static_cast<uchar>(px[2] * 0.299 + px[1] * 0.587 + px[0] * 0.114);
            uchar alpha = px[3];

            // Set the pixel in the converted image
            converted_img.at<cv::Vec4b>(y, x) = cv::Vec4b(intensity, intensity, intensity, alpha);
        }
    }
    return converted_img;
}

int main() {
    // Load the image (make sure the image has 4 channels RGBA)
    cv::Mat original = cv::imread("path/to/your/image.png", cv::IMREAD_UNCHANGED);

    if (original.empty()) {
        std::cerr << "Could not open or find the image" << std::endl;
        return -1;
    }

    // Convert the image
    cv::Mat converted_img = convert_image_to_IA8(original);

    // Save the converted image
    cv::imwrite("path/to/your/converted_image.png", converted_img);

    // Display the original and converted images
    cv::imshow("Original Image", original);
    cv::imshow("Converted Image", converted_img);

    // Wait for a key press indefinitely
    cv::waitKey(0);

    return 0;
}
