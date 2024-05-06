#include <iostream>
#include <thread>
#include <vector>
#include <queue>
#include <mutex>
#include <list>
#include <condition_variable>


// sample Object to be managed by the pool
class Object {
public:
    void activate() {
        std::cout << "Object is activating..." << std::endl;
    }
    void deactivate() {
        std::cout << "Object is deactivating..." << std::endl;
    }
    void update() {
        std::cout << "Object is updating..." << std::endl;
    }
};

// Object Pool that manages objects with active/inactive states including running updates on the objects in use
class ObjectPool {
private:
    bool expandable; // whether or not the pool can expand in size
    std::queue<Object*> objects; // available objects
    std::list<Object*> activeObjects; // active objects for updating
    std::thread updateThread; // update loop thread
    std::mutex mutex; // mutex
    std::condition_variable releaseMonitor; // object release tracker
    bool stopUpdateFlag; // stop flag for the update thread

    // Update loop: calls update for all active Objects 
    void updateLoop() {
        while (!stopUpdateFlag) {
            std::lock_guard<std::mutex> lock(mutex);
            for (Object* obj : activeObjects) {
                obj->update();
            }
        }
    }

public:
    ObjectPool(int numObjects) : expandable(false), stopUpdateFlag(false) {
        for (int i = 0; i < numObjects; ++i) {
            objects.push(new Object());
        }

        // Start the update thread
        updateThread = std::thread(&ObjectPool::updateLoop, this);
    }
    ObjectPool(int numObjects, bool expandable) : expandable(expandable), stopUpdateFlag(false) {
        for (int i = 0; i < numObjects; ++i) {
            objects.push(new Object());
        }

        // Start the update thread
        updateThread = std::thread(&ObjectPool::updateLoop, this);
    }

    ~ObjectPool() {
        // Stop update thread
        stopUpdateFlag = true;
        releaseMonitor.notify_all();
        updateThread.join();

        // Clean up the objects
        while (!activeObjects.empty()) {
            releaseObject(activeObjects.front());
        }
        while (!objects.empty()) {
            delete objects.front();
            objects.pop();
        }
    }

    // Request an object from the pool, either wait for an object to be available or expand the pool
    Object* requestObject() {
        // if the pool can't expand then wait for an object to be available if empty, otherwise make a new one
        if (!expandable) {
            std::unique_lock<std::mutex> lock(mutex);
            while (objects.empty()) {
                // Wait until an object is available
                releaseMonitor.wait(lock);
            }
        }
        else {
            if (objects.empty()) {
                objects.push(new Object());
            }
        }
        Object* obj = objects.front();
        objects.pop();
        obj->activate();
        activeObjects.push_back(obj);
        return obj;
    }

    // Return an object back to the pool
    void releaseObject(Object* obj) {
        std::lock_guard<std::mutex> lock(mutex);
        obj->deactivate();
        objects.push(obj);
        activeObjects.remove(obj);
        releaseMonitor.notify_one();
    }
};

