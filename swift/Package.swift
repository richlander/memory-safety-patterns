// swift-tools-version: 6.2
// The swift-tools-version declares the minimum version of Swift required to build this package.

import PackageDescription

let package = Package(
    name: "MemorySafety",
    products: [
        .library(
            name: "MemoryLib",
            targets: ["MemoryLib"]
        ),
        .executable(
            name: "MemoryApp",
            targets: ["MemoryApp"]
        ),
    ],
    targets: [
        .target(
            name: "MemoryLib",
            swiftSettings: [
                // Enable Swift 6.2 strict memory safety checking
                // This produces warnings for unsafe code that only the library author sees
                .strictMemorySafety()
            ]
        ),
        .executableTarget(
            name: "MemoryApp",
            dependencies: ["MemoryLib"],
            swiftSettings: [
                // App also opts into strict memory safety
                .strictMemorySafety()
            ]
        ),
    ]
)
