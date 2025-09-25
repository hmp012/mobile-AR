//
//  ContentView.swift
//  ARProject
//
//  Created by Hugo Madrid Peñarrubia on 25/09/2025.
//

import SwiftUI
import RealityKit
import TestProject

struct ContentView : View {
    var body: some View {
        RealityView { content in
            // 2. Asynchronously load the scene from the package.
            do {
                // "Scene" is the default name of the root entity in a new Reality Composer Pro project.
                // If you renamed it in the editor, change the name here to match.
                let sceneEntity = try await Entity(named: "Scene", in: testProjectBundle)
                
                // 3. Add the loaded scene entity to your AR view's content.
                content.add(sceneEntity)
                
            } catch {
                // 4. Handle potential errors, e.g., if the scene file can't be found.
                print("Error loading scene from Reality Composer Pro package: \(error)")
            }
        }
        .edgesIgnoringSafeArea(.all)
    }
}

#Preview {
    ContentView()
}
