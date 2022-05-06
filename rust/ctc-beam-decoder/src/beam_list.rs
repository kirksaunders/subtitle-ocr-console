use std::{
    collections::HashMap,
    collections::hash_map::Entry,
    ops::{Index, IndexMut},
};

use crate::log_space::LogSpace;

#[derive(Clone, Copy)]
pub struct BeamProbability {
    pub label: LogSpace,
    pub blank: LogSpace,
    pub total: LogSpace,
}

impl Default for BeamProbability {
    fn default() -> Self {
        Self {
            label: LogSpace(f32::NEG_INFINITY),
            blank: LogSpace(f32::NEG_INFINITY),
            total: LogSpace(f32::NEG_INFINITY),
        }
    }
}

pub struct BeamEntry {
    pub old_p: BeamProbability,
    pub new_p: BeamProbability,
    pub label: i32,
    pub parent: usize,
    children: HashMap<i32, usize>,
}

impl Default for BeamEntry {
    fn default() -> Self {
        Self {
            old_p: Default::default(),
            new_p: Default::default(),
            label: i32::MAX,
            parent: usize::MAX,
            children: HashMap::new(),
        }
    }
}

impl BeamEntry {
    pub fn is_active(&self) -> bool {
        self.new_p.total != LogSpace(f32::NEG_INFINITY)
    }
}

impl PartialEq for BeamEntry {
    fn eq(&self, other: &Self) -> bool {
        self.new_p.total == other.new_p.total
    }
}

impl PartialOrd for BeamEntry {
    fn partial_cmp(&self, other: &Self) -> Option<std::cmp::Ordering> {
        self.new_p.total.partial_cmp(&other.new_p.total)
    }
}

pub struct BeamList {
    capacity: usize,
    entries: Vec<BeamEntry>,
    heap: Vec<usize>,
}

impl BeamList {
    pub fn new(capacity: usize) -> Self {
        Self {
            capacity: capacity,
            entries: Vec::new(),
            heap: Vec::with_capacity(capacity as usize),
        }
    }

    pub fn clone_heap(&self) -> Vec<usize> {
        self.heap.clone()
    }

    pub fn clear(&mut self) {
        self.heap.clear();
    }

    fn new_node(&mut self) -> usize {
        self.entries.push(Default::default());
        self.entries.len() - 1
    }

    pub fn add_root(&mut self, label: i32) {
        assert!(
            self.heap.is_empty(),
            "Tried to insert root node into non-empty heap"
        );

        let root_idx = self.new_node();
        let root = &mut self.entries[root_idx];
        root.new_p.blank = LogSpace(0.0);
        root.new_p.total = LogSpace(0.0);
        root.label = label;
        self.push(root_idx);
    }

    pub fn deactivate(&mut self, entry_idx: usize) {
        // Reset probability
        self.entries[entry_idx].new_p = Default::default();
    }

    pub fn get_child(&mut self, parent: usize, label: i32) -> usize {
        if let Entry::Occupied(idx) = self.entries[parent].children.entry(label) {
            *idx.get()
        } else {
            let new_idx = self.new_node();
            self.entries[new_idx].label = label;
            self.entries[new_idx].parent = parent;
            self.entries[parent].children.insert(label, new_idx);
            new_idx
        }
    }

    pub fn get_seq(&self, mut entry_idx: usize) -> Vec<i32> {
        let mut seq = Vec::new();

        // Note: This skips the root entry. That is desired
        while self.entries[entry_idx].parent != usize::MAX {
            seq.push(self.entries[entry_idx].label);
            entry_idx = self.entries[entry_idx].parent;
        }

        seq.into_iter().rev().collect()
    }

    pub fn len(&self) -> usize {
        self.heap.len()
    }

    pub fn min(&self) -> usize {
        self.heap[0]
    }

    pub fn push(&mut self, entry_idx: usize) -> Option<usize> {
        if self.heap.len() < self.capacity {
            self.heap.push(entry_idx);
            self.sift_up(self.heap.len() - 1);
            None
        } else {
            let removed = self.heap[0];
            self.heap[0] = entry_idx;
            self.sift_down(0);
            Some(removed)
        }
    }

    fn sift_up(&mut self, mut idx: usize) {
        while idx > 0 {
            let parent = (idx - 1) / 2;

            // Heap property already holds
            if self.entries[self.heap[idx]] >= self.entries[self.heap[parent]] {
                break;
            }

            self.heap.swap(idx, parent);
            idx = parent;
        }
    }

    fn sift_down(&mut self, mut idx: usize) {
        while idx < self.heap.len() {
            let left = 2 * idx + 1;
            let right = left + 1;

            // If no children, quit
            if left >= self.heap.len() {
                break;
            }

            // Get child with smaller value
            let mut child = left;
            if right < self.heap.len() {
                if self.entries[self.heap[right]] < self.entries[self.heap[left]] {
                    child = right;
                }
            }

            // Heap property already holds
            if self.entries[self.heap[idx]] <= self.entries[self.heap[child]] {
                break;
            }

            self.heap.swap(idx, child);
            idx = child;
        }
    }
}

impl Index<usize> for BeamList {
    type Output = BeamEntry;

    fn index(&self, index: usize) -> &Self::Output {
        &self.entries[index]
    }
}

impl IndexMut<usize> for BeamList {
    fn index_mut(&mut self, index: usize) -> &mut Self::Output {
        &mut self.entries[index]
    }
}
