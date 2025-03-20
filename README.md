# ZhoConverterAvaMvvm

ZhoConverterAvaMvvm is a Chinese text conversion application built using Avalonia and the MVVM design pattern. It leverages the OpenCC and Jieba libraries to provide functionalities such as simplified and traditional Chinese conversion, as well as Chinese word segmentation.

## Features

- **Chinese Conversion**: Convert between simplified and traditional Chinese text.
- **Word Segmentation**: Perform Chinese word segmentation to analyze text.

## Dependencies

- [Avalonia](https://avaloniaui.net/): Cross-platform .NET UI framework.
- [OpenCC](https://github.com/BYVoid/OpenCC): Open Chinese Convert library for conversions between Traditional and Simplified Chinese.
- [JiebaNet](https://github.com/anderscui/jieba.NET): .NET port of the Jieba Chinese text segmentation library.
- [Newtonsoft.Json](https://www.newtonsoft.com/json): Popular high-performance JSON framework for .NET.

## Getting Started

1. **Clone the repository**:

   ```bash
   git clone https://github.com/laisuk/ZhoConverterAvaMvvm.git

2. **Navigate to the project directory**:

    ```bash
    cd ZhoConverterAvaMvvm

3. **Restore dependencies**:
    ```bash
   dotnet restore

4. **Build the project**:
    ```bash
    dotnet build
   
5. **Run the application**:
    ```bash
    dotnet run
   
# Usage

1. **Chinese Conversion**:
- Input the text you wish to convert.
- Select the desired conversion direction (e.g., Simplified to Traditional).
- Click the "Convert" button to see the results.

2. **Word Segmentation**:
- Input the Chinese text you want to segment.
- Click the "Segment" button to view the segmented words.

# Contributing
Contributions are welcome! Please fork the repository and submit a pull request for any enhancements or bug fixes.

# License
This project is licensed under the MIT License. See the [LICENSE](./LICENSE) file for details.


# Acknowledgements

- [OpenCC](https://github.com/BYVoid/OpenCC) for Chinese text conversion.
- [JiebaNet](https://github.com/anderscui/jieba.NET) for Chinese word segmentation.
- [Avalonia](https://avaloniaui.net/) for the cross-platform UI framework.
- [Newtonsoft.Json](https://www.newtonsoft.com/json) for JSON parsing.


 

 